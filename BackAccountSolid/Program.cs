using System;
using System.Collections.Generic;

namespace BankAccountApp
{
    // Interfaces: [S] Separated Interfaces
    // [O] - IAccount allows new account types
    // Interfaces broken down: [I] - Basic account
    public interface IAccount
    {
        string AccountNumber { get; }
        string OwnerName { get; }
        decimal GetBalance();
    }

    // Interfaces broken down: [I] - Basic transactions
    public interface ITransactionOperations
    {
        void Deposit(decimal amount);
        bool Withdraw(decimal amount);
    }

    // Interfaces broken down: [I] - Transfer
    public interface ITransferOperations
    {
        bool Transfer(IAccount recipient, decimal amount);
    }

    // Interfaces broken down: [I] - Storage
    public interface IAccountRepository
    {
        void AddAccount(IAccount account);
        IAccount GetAccount(string accountNumber);
        bool AccountExists(string accountNumber);
    }

    // Interfaces broken down: [I] - Banking Operations
    public interface IBankService
    {
        void CreateAccount(string accountNumber, string ownerName, decimal initialBalance);
        void Deposit(string accountNumber, decimal amount);
        bool Withdraw(string accountNumber, decimal amount);
        bool Transfer(string sourceAccountNumber, string destinationAccountNumber, decimal amount);
        decimal GetBalance(string accountNumber);
    }

    // [S] Manages account information
    public class Account : IAccount, ITransactionOperations, ITransferOperations
    {
        public string AccountNumber { get; }
        public string OwnerName { get; }
        private decimal Balance { get; set; }

        public Account(string accountNumber, string ownerName, decimal initialBalance = 0)
        {
            ValidateAccountCreation(accountNumber, ownerName, initialBalance);

            AccountNumber = accountNumber;
            OwnerName = ownerName;
            Balance = initialBalance;
        }

        private void ValidateAccountCreation(string accountNumber, string ownerName, decimal initialBalance)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                throw new ArgumentException("Account number cannot be empty");

            if (string.IsNullOrWhiteSpace(ownerName))
                throw new ArgumentException("Owner name cannot be empty");

            if (initialBalance < 0)
                throw new ArgumentException("Initial balance cannot be negative");
        }

        public decimal GetBalance() => Balance;

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Deposit amount must be positive");

            Balance += amount;
        }

        public bool Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Withdrawal amount must be positive");

            if (amount > Balance)
                return false;

            Balance -= amount;
            return true;
        }

        public bool Transfer(IAccount recipient, decimal amount)
        {
            if (recipient == null)
                throw new ArgumentNullException(nameof(recipient));

            if (amount <= 0)
                throw new ArgumentException("Transfer amount must be positive");

            if (amount > Balance)
                return false;

            Balance -= amount;
            ((ITransactionOperations)recipient).Deposit(amount);
            return true;
        }
    }

    // Repository: [S]-Handles account storage/retrieval
    public class AccountRepository : IAccountRepository
    {
        private readonly Dictionary<string, IAccount> accounts = new Dictionary<string, IAccount>();

        public void AddAccount(IAccount account)
        {
            if (accounts.ContainsKey(account.AccountNumber))
                throw new ArgumentException("Account number already exists");

            accounts[account.AccountNumber] = account;
        }

        public IAccount GetAccount(string accountNumber)
        {
            if (!accounts.ContainsKey(accountNumber))
                throw new KeyNotFoundException("Account not found");

            return accounts[accountNumber];
        }

        public bool AccountExists(string accountNumber)
        {
            return accounts.ContainsKey(accountNumber);
        }
    }

    // Bank Service: [S] Handle Bank Operations
    public class BankService : IBankService
    {
        private readonly IAccountRepository accountRepository;
        private readonly ITransactionLogger transactionLogger;

        // [D] - Dependencies are injected through constructors
        public BankService(IAccountRepository accountRepository, ITransactionLogger transactionLogger)
        {
            this.accountRepository = accountRepository;
            this.transactionLogger = transactionLogger;
        }

        public void CreateAccount(string accountNumber, string ownerName, decimal initialBalance)
        {
            var account = new Account(accountNumber, ownerName, initialBalance);
            accountRepository.AddAccount(account);
            transactionLogger.LogTransaction("Account Creation", accountNumber, initialBalance);
        }

        public void Deposit(string accountNumber, decimal amount)
        {
            var account = accountRepository.GetAccount(accountNumber);
            ((ITransactionOperations)account).Deposit(amount);
            transactionLogger.LogTransaction("Deposit", accountNumber, amount);
        }

        public bool Withdraw(string accountNumber, decimal amount)
        {
            var account = accountRepository.GetAccount(accountNumber);
            bool success = ((ITransactionOperations)account).Withdraw(amount);
            if (success)
            {
                transactionLogger.LogTransaction("Withdrawal", accountNumber, -amount);
            }
            return success;
        }

        public bool Transfer(string sourceAccountNumber, string destinationAccountNumber, decimal amount)
        {
            var sourceAccount = accountRepository.GetAccount(sourceAccountNumber);
            var destinationAccount = accountRepository.GetAccount(destinationAccountNumber);

            bool success = ((ITransferOperations)sourceAccount).Transfer(destinationAccount, amount);
            if (success)
            {
                transactionLogger.LogTransaction("Transfer Out", sourceAccountNumber, -amount);
                transactionLogger.LogTransaction("Transfer In", destinationAccountNumber, amount);
            }
            return success;
        }

        public decimal GetBalance(string accountNumber)
        {
            var account = accountRepository.GetAccount(accountNumber);
            return account.GetBalance();
        }
    }

    // Transaction Logger: [S] Handles transaction logging
    // Interfaces broken down: [I] - Logging
    public interface ITransactionLogger
    {
        void LogTransaction(string type, string accountNumber, decimal amount);
    }

    public class ConsoleTransactionLogger : ITransactionLogger
    {
        public void LogTransaction(string type, string accountNumber, decimal amount)
        {
            Console.WriteLine($"{DateTime.Now}: {type} - Account: {accountNumber}, Amount: {amount:C}");
        }
    }

    // UI: [S]-Interface Interactions
    public class BankConsoleUI
    {
        private readonly IBankService bankService;

        // [D] - Dependencies are injected through constructors
        public BankConsoleUI(IBankService bankService)
        {
            this.bankService = bankService;
        }

        public void Start()
        {
            bool running = true;
            while (running)
            {
                try
                {
                    DisplayMenu();
                    string choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            HandleCreateAccount();
                            break;
                        case "2":
                            HandleDeposit();
                            break;
                        case "3":
                            HandleWithdrawal();
                            break;
                        case "4":
                            HandleTransfer();
                            break;
                        case "5":
                            HandleCheckBalance();
                            break;
                        case "6":
                            running = false;
                            Console.WriteLine("Thank you for using our banking application!");
                            break;
                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }

        private void DisplayMenu()
        {
            Console.WriteLine("\nPlease select an option:");
            Console.WriteLine("1. Create new account");
            Console.WriteLine("2. Deposit");
            Console.WriteLine("3. Withdraw");
            Console.WriteLine("4. Transfer");
            Console.WriteLine("5. Check balance");
            Console.WriteLine("6. Exit");
        }

        private void HandleCreateAccount()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            Console.Write("Enter account holder name: ");
            string ownerName = Console.ReadLine();

            Console.Write("Enter initial balance: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal initialBalance))
            {
                bankService.CreateAccount(accountNumber, ownerName, initialBalance);
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
        }

        private void HandleDeposit()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            Console.Write("Enter amount to deposit: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                bankService.Deposit(accountNumber, amount);
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
        }

        private void HandleWithdrawal()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            Console.Write("Enter amount to withdraw: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                if (!bankService.Withdraw(accountNumber, amount))
                {
                    Console.WriteLine("Withdrawal failed due to insufficient funds.");
                }
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
        }

        private void HandleTransfer()
        {
            Console.Write("Enter source account number: ");
            string sourceAccount = Console.ReadLine();

            Console.Write("Enter destination account number: ");
            string destAccount = Console.ReadLine();

            Console.Write("Enter amount to transfer: ");
            if (decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                if (!bankService.Transfer(sourceAccount, destAccount, amount))
                {
                    Console.WriteLine("Transfer failed due to insufficient funds.");
                }
            }
            else
            {
                Console.WriteLine("Invalid amount entered.");
            }
        }

        private void HandleCheckBalance()
        {
            Console.Write("Enter account number: ");
            string accountNumber = Console.ReadLine();

            decimal balance = bankService.GetBalance(accountNumber);
            Console.WriteLine($"Current balance: {balance:C}");
        }
    }

    // Program Entry Point
    class Program
    {
        static void Main(string[] args)
        {
            // Setup dependency injection
            IAccountRepository accountRepository = new AccountRepository();
            ITransactionLogger transactionLogger = new ConsoleTransactionLogger();
            IBankService bankService = new BankService(accountRepository, transactionLogger);

            // Create and start UI
            var bankUI = new BankConsoleUI(bankService);
            bankUI.Start();
        }
    }
}