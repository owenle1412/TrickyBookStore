using System;
using System.Linq;
using TrickyBookStore.Services.Books;
using TrickyBookStore.Services.Customers;
using TrickyBookStore.Services.PurchaseTransactions;
using TrickyBookStore.Services.Subscriptions;
using TrickyBookStore.Models;
using System.Collections.Generic;

namespace TrickyBookStore.Services.Payment
{
    public class PaymentService : IPaymentService
    {
        ICustomerService CustomerService { get; }
        IPurchaseTransactionService PurchaseTransactionService { get; }
        IBookService BookService { get; }
        ISubscriptionService SubscriptionService { get; }

        public PaymentService(ICustomerService customerService, ISubscriptionService subscriptionService,
            IPurchaseTransactionService purchaseTransactionService, IBookService bookService)
        {
            CustomerService = customerService;
            PurchaseTransactionService = purchaseTransactionService;
            BookService = bookService;
            SubscriptionService = subscriptionService;
        }

        public double GetPaymentAmount(long customerId, int month, int year)
        {
            double totalAmount = 0.0;
            double oldBookAmount = 0.0;
            double newBookAmount = 0.0;

            Customer customer = CustomerService.GetCustomerById(customerId);
            IList<PurchaseTransaction> purchaseTransactions = PurchaseTransactionService.GetPurchaseTransactions(customerId, month, year);
            

            // If there is no purchase transaction
            if (purchaseTransactions == null)
            {
                return 0;
            }
            IList<Subscription> customerSubscriptions = SubscriptionService.GetSubscriptions(customer.SubscriptionIds.ToArray());
            long[] listBooksId = purchaseTransactions.Select(x => x.BookId).ToArray();
            IList<Book> books = BookService.GetBooks(listBooksId);
            List<int?> customerCatAccBookCatList = customerSubscriptions
                .Where(s => s.SubscriptionType == SubscriptionTypes.CategoryAddicted)
                .Select(bc => bc.BookCategoryId).ToList();
            bool hasPaidAcc = customerSubscriptions.Any(s => s.SubscriptionType == SubscriptionTypes.Paid);
            bool hasPremAcc = customerSubscriptions.Any(s => s.SubscriptionType == SubscriptionTypes.Premium);

            int bookCount = books.Count;

            for (int i = 0; i < bookCount; i++)
            {
                if (books[i].IsOld == true) // old books
                {
                    if (hasPremAcc)
                    {
                        oldBookAmount += 0;
                    }
                    else
                    {
                        // old book belongs to cat of CatAdd Acc
                        if (customerCatAccBookCatList.Contains(books[i].CategoryId))
                        {
                            oldBookAmount += 0;
                        }
                        else
                        {
                            if (hasPaidAcc)
                            {
                                oldBookAmount += (books[i].Price * 0.05);
                            }
                            else
                            {
                                oldBookAmount += (books[i].Price *0.9);
                            }
                        }
                    }
                }
                else // new books
                {
                    Dictionary<int, int> discountBooksPerCat = new Dictionary<int, int>();
                    foreach (int cat in customerCatAccBookCatList)
                    {
                        discountBooksPerCat.Add(cat, 3);
                    }
                    int numberOfBooksPremDiscount = 3;
                    int numberOfBooksPaidDiscount = 3;
                    if (discountBooksPerCat.ContainsKey(books[i].CategoryId))
                    {
                        if (discountBooksPerCat[books[i].CategoryId] > 0)
                        {
                            newBookAmount += (books[i].Price * 0.85);
                            discountBooksPerCat[books[i].CategoryId] -= 1;
                        }
                        else if (hasPremAcc)
                        {
                            if (numberOfBooksPremDiscount > 0)
                            {
                                newBookAmount += (books[i].Price * 0.85);
                                numberOfBooksPremDiscount -= 1;
                            }
                        }
                        else if (hasPaidAcc)
                        {
                            if (numberOfBooksPaidDiscount > 0)
                            {
                                newBookAmount += (books[i].Price * 0.95);
                                numberOfBooksPaidDiscount -= 1;
                            }
                        }
                        else
                        {
                            newBookAmount += books[i].Price;
                        }
                        
                    }
                    else if (hasPremAcc)
                    {
                        if (numberOfBooksPremDiscount > 0)
                        {
                            newBookAmount += (books[i].Price * 0.85);
                            numberOfBooksPremDiscount -= 1;
                        }
                        else
                        {
                            newBookAmount += books[i].Price;
                        }
                    }
                    else if (hasPaidAcc)
                    {
                        if (numberOfBooksPaidDiscount > 0)
                        {
                            newBookAmount += (books[i].Price * 0.95);
                            numberOfBooksPaidDiscount -= 1;
                        }
                        else
                        {
                            newBookAmount += books[i].Price;
                        }
                    }
                    else
                    {
                        newBookAmount += books[i].Price;
                    }
                }
            }

            totalAmount = oldBookAmount + newBookAmount;

            return totalAmount;
        }
    }
}
