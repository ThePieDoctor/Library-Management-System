// Library Management System
// Author: [Evan Barrett]

using System.Globalization;
using LibraryManagementSystem.Core;
using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using LibraryManagementSystem.UI;
using Microsoft.Data.Sqlite;

public class Program
{
    // Starts the Library Management System application and initializes the database.
    public static void Main(string[] args)
    {
        string databasePath = Path.Combine(Environment.CurrentDirectory, "library.db");
        IDataService dataService = new SqliteDataService(databasePath);
        Library library = new(dataService);
        ConsoleUI ui = new(library);
        ui.StartProgram();
    }
}

// Shows different genres of books available in the library
namespace LibraryManagementSystem.Models
{
        public enum BookGenre
    {
        Fiction,
        NonFiction,
        SciFi,
        Fantasy,
        Mystery
    }
// Represents a library item with ID and calculates late fees
    public abstract class LibraryItem
    {
        public string ItemID { get; set; }

        protected LibraryItem()
        {
            ItemID = "default-item-id";
        }

        protected LibraryItem(string itemID)
        {
            ItemID = itemID;
        }

        public abstract decimal CalculateLateFeeRate();
    }
    // Represents a book in the library with title, author, genre, and availability status.
    public class Book : LibraryItem
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public BookGenre Genre { get; set; }
        public bool Available { get; set; }

        public Book() : base("default-book-id")
        {
            Title = "Unknown";
            Author = "Unknown";
            Genre = BookGenre.Fiction;
            Available = false;
        }
        // New instance of the book with set ID. Book availability is set to true by default.
        public Book(string itemID, string title, string author, BookGenre genre) : base(itemID)
        {
            Title = title;
            Author = author;
            Genre = genre;
            Available = true;
        }

        public bool IsAvailable()
        {
            return Available;
        }

        public void CheckOut()
        {
            Available = false;
        }

        public void Return()
        {
            Available = true;
        }

        public override decimal CalculateLateFeeRate()
        {
            return 0.25m;
        }
        // Returns a string of the book's details, including ID, title, author, genre, and availability status.
        public string GetDetailsString()
        {
            string availability = Available ? "Available" : "Checked Out";
            return $"[{ItemID}] '{Title}' by {Author} ({Genre}) - {availability}";
        }
    }
    // Represents a person in the system with name and ID.
    public abstract class Person
    {
        public string Name { get; set; }
        public string ID { get; set; }
        // Protected so only a patron can call the constructor.
        protected Person()
        {
            Name = "Default";
            ID = "Default";
        }
        protected Person(string name, string id)
        {
            Name = name;
            ID = id;
        }
        public string GetID()
        {
            return ID;
        }
        public abstract string GetDetailsString();
    }
    // Represents a patron with a list of checked-out books and total fines.
    public class Patron : Person
    {
        // List of book IDs that the patron has checked out. Max of 5 at a time
        private readonly List<string> _bookIDsCheckedOut = new();
        private const int MAX_BOOKS = 5;

        public decimal TotalFines { get; set; }

        public Patron() : base()
        {
            TotalFines = 0;
        }

        public Patron(string name, string id) : base(name, id)
        {
            TotalFines = 0;
        }
        // Checks if the patron can check out more books based on the maximum limit.
        public bool CanCheckout()
        {
            return _bookIDsCheckedOut.Count < MAX_BOOKS;
        }
        // Adds a fine amount to the patron's total fines.
        public void AddFine(decimal amount)
        {
            TotalFines += amount;
        }
        // Adds a book ID to the list of checked-out books
        public void AddBook(string bookID)
        {
            if (!_bookIDsCheckedOut.Contains(bookID))
            {
                _bookIDsCheckedOut.Add(bookID);
            }
        }
        // Removes a book ID from the list of checked-out books
        public void RemoveBook(string bookID)
        {
            _bookIDsCheckedOut.Remove(bookID);
        }
        // Returns a list of book IDs that the patron has checked out.
        public List<string> GetCheckedOutBookIDs()
        {
            return _bookIDsCheckedOut;
        }
        // Returns a string of the patron's details
        public override string GetDetailsString()
        {
            return $"Patron: {Name} (ID: {ID}), Books Checked Out: {_bookIDsCheckedOut.Count}, Total Fines: {TotalFines:C}";
        }
    }
    // Represents a loan of a book to a patron with due date
    public class Loan
    {
        public string BookID { get; set; }
        public string PatronID { get; set; }
        public DateTime DueDate { get; set; }

        public Loan()
        {
            BookID = "default";
            PatronID = "default";
            DueDate = DateTime.Now;
        }

        public Loan(string bookID, string patronID)
        {
            BookID = bookID;
            PatronID = patronID;
            DueDate = DateTime.Now.AddDays(14);
        }
        // Checks if the loan is late
        public bool IsLate()
        {
            return DateTime.Now > DueDate;
        }
        // Calculates the number of days the loan is late
        public int CalculateDaysLate()
        {
            if (!IsLate())
            {
                return 0;
            }

            TimeSpan timeSpan = DateTime.Now - DueDate;
            return (int)Math.Ceiling(timeSpan.TotalDays);
        }
    }

    public class Fine
    {
        public decimal CalculateTotalFine(LibraryItem item, int daysLate)
        {
            return item.CalculateLateFeeRate() * daysLate;
        }
    }
    public record LoanSummary(string BookTitle, string PatronName, DateTime DueDate);
}
// Represents the core library management system with catalog, patrons, and loans.
namespace LibraryManagementSystem.Core
{
    // Creates a data structure to hold the library's data
    public class LibraryData
    {
        public List<Book> Catalog { get; set; } = new();
        public List<Patron> Patrons { get; set; } = new();
        public List<Loan> Loans { get; set; } = new();
    }
    // Represents the library private data being stored, handling patrons, books, and loans.
    public class Library
    {
        private List<Book> _catalog = new();
        private List<Patron> _patrons = new();
        private List<Loan> _loans = new();

        private readonly IDataService _dataService;
        private readonly Fine _fineCalculator;

        // Initializes the library with a data service for persistence and a fine calculator.
        public Library(IDataService dataService)
        {
            _dataService = dataService;
            _fineCalculator = new Fine();
            LoadData();
        }
        // Adds a new patron to the library if the ID is unique.
        public bool AddPatron(string name, string id)
        {
            if (_patrons.Find(patron => patron.GetID() == id) != null)
            {
                return false;
            }

            Patron patron = new(name, id);
            _dataService.AddPatron(patron);
            _patrons.Add(patron);
            return true;
        }
        // Adds a new book to the library catalog if the ID is unique.
        public bool AddBook(string itemID, string title, string author, BookGenre genre)
        {
            if (_catalog.Find(book => book.ItemID == itemID) != null)
            {
                return false;
            }

            Book book = new(itemID, title, author, genre);
            _dataService.AddBook(book);
            _catalog.Add(book);
            return true;
        }
        // Loads the library data from SQLite database and seeds new data if none exists.
        public void LoadData()
        {
            Console.WriteLine("Loading library data...");
            LibraryData data = _dataService.Load();
            _catalog = data.Catalog;
            _patrons = data.Patrons;
            _loans = data.Loans;

            foreach (Loan loan in _loans)
            {
                _patrons.Find(patron => patron.GetID() == loan.PatronID)?.AddBook(loan.BookID);
            }

            if (_catalog.Count == 0 && _patrons.Count == 0)
            {
                Console.WriteLine("No data found. Seeding new data...");
                SeedData();
            }
        }
        // Checks out a book to a patron if both exist, the patron can check out more books, and the book is available.
        public bool CheckOutBook(string patronID, string bookID)
        {
            Patron? patron = _patrons.Find(item => item.GetID() == patronID);
            Book? book = _catalog.Find(item => item.ItemID == bookID);
            if (patron == null)
            {
                Console.WriteLine("Error: Patron not found.");
                return false;
            }
            if (book == null)
            {
                Console.WriteLine("Error: Book not found.");
                return false;
            }
            if (!patron.CanCheckout())
            {
                Console.WriteLine("Error: Patron has reached the maximum checkout limit.");
                return false;
            }
            if (!book.IsAvailable())
            {
                Console.WriteLine("Error: Book is currently unavailable.");
                return false;
            }
            // Create a new loan record
            Loan newLoan = new(bookID, patronID);
            _dataService.CheckOutBook(newLoan);
            book.CheckOut();
            _loans.Add(newLoan);
            patron.AddBook(bookID);

            Console.WriteLine($"Success: '{book.Title}' checked out to {patron.Name}.");
            return true;
        }
        // Returns a book from a patron, calculates fines if late 
        public void ReturnBook(string patronID, string bookID)
        {
            Patron? patron = _patrons.Find(item => item.GetID() == patronID);
            Book? book = _catalog.Find(item => item.ItemID == bookID);
            Loan? loan = _loans.Find(item => item.BookID == bookID && item.PatronID == patronID);

            if (patron == null || book == null || loan == null)
            {
                Console.WriteLine("Error: Could not find matching loan record for this return.");
                return;
            }

            decimal fineAmount = 0;
            if (loan.IsLate())
            {
                int daysLate = loan.CalculateDaysLate();
                fineAmount = _fineCalculator.CalculateTotalFine(book, daysLate);
                Console.WriteLine($"Book is {daysLate} days late! A fine of {fineAmount:C} has been added.");
            }

            _dataService.ReturnBook(loan, fineAmount);
            book.Return();
            patron.AddFine(fineAmount);
            _loans.Remove(loan);
            patron.RemoveBook(bookID);

            Console.WriteLine($"Success: '{book.Title}' returned by {patron.Name}.");
        }
        public List<Book> Search(string title)
        {
            return _catalog.FindAll(book => book.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
        }
        // Searches for books in the catalog by genre 
        public List<Book> Search(BookGenre genre)
        {
            return _catalog.FindAll(book => book.Genre == genre);
        }
        // Gets the entire catalog of books
        public List<Book> GetCatalog()
        {
            return _catalog;
        }

        public Patron? GetPatron(string patronID)
        {
            return _patrons.Find(patron => patron.GetID() == patronID);
        }

        public List<LoanSummary> GetActiveLoans()
        {
            return _dataService.GetActiveLoans();
        }
        // Seeds the library with initial patrons and books if no data exists.
        private void SeedData()
        {
            _patrons = new()
            {
                new Patron("Evan Barrett", "p100"),
                new Patron("Guest", "p001"),
                new Patron("Joseph Smith", "p531")
            };

            _catalog = new()
            {
                new Book("b001", "Harry Potter and the Sorcerer's Stone", "J.K. Rowling", BookGenre.Fantasy),
                new Book("b002", "The Hobbit", "J.R.R. Tolkien", BookGenre.Fantasy),
                new Book("b003", "Percy Jackson: The Lightning Thief", "Rick Riordan", BookGenre.Fantasy),
                new Book("b004", "Diary of a Wimpy Kid", "Jeff Kinney", BookGenre.Fiction),
                new Book("b005", "The Cat in the Hat", "Dr. Seuss", BookGenre.Fiction),
                new Book("b006", "The Miracle of Forgiveness", "Spencer W. Kimball", BookGenre.NonFiction),
                new Book("b007", "The Book Thief", "Markus Zusak", BookGenre.NonFiction),
                new Book("b008", "The Great Gatsby", "F. Scott Fitzgerald", BookGenre.Fiction),
                new Book("b009", "The Maze Runner", "James Dashner", BookGenre.SciFi)
            };

            _loans = new();

            foreach (Patron patron in _patrons)
            {
                _dataService.AddPatron(patron);
            }

            foreach (Book book in _catalog)
            {
                _dataService.AddBook(book);
            }
        }
    }
}
// Represents the data service interface for loading and saving library data.
namespace LibraryManagementSystem.Services
{
    public interface IDataService
    {
        LibraryData Load();
        void AddBook(Book book);
        void AddPatron(Patron patron);
        void CheckOutBook(Loan loan);
        void ReturnBook(Loan loan, decimal fineAmount);
        List<LoanSummary> GetActiveLoans();
    }
    // Implements the Data Service interface using SQLite for data persistence.
    public class SqliteDataService : IDataService
    {
        private readonly string _connectionString;

        public SqliteDataService(string databasePath)
        {
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = databasePath,
                ForeignKeys = true
            }.ToString();

            InitializeDatabase();
        }
        // Loads the library data from the SQLite database
        public LibraryData Load()
        {
            using SqliteConnection connection = OpenConnection();
            LibraryData data = new();
            // Load books from the database using SQL
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT item_id, title, author, genre, available
                    FROM Books
                    ORDER BY title;
                    """;
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    data.Catalog.Add(new Book(
                        reader.GetString(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        (BookGenre)reader.GetInt32(3))
                    {
                        Available = reader.GetInt64(4) == 1
                    });
                }
            }
            // Load patrons from the database using SQL
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT patron_id, name, total_fines_cents
                    FROM Patrons
                    ORDER BY name;
                    """;
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    data.Patrons.Add(new Patron(reader.GetString(1), reader.GetString(0))
                    {
                        TotalFines = reader.GetInt64(2) / 100m
                    });
                }
            }
            // Load loans from the database using SQL
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    SELECT book_id, patron_id, due_date
                    FROM Loans
                    ORDER BY due_date;
                    """;
                using SqliteDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    data.Loans.Add(new Loan
                    {
                        BookID = reader.GetString(0),
                        PatronID = reader.GetString(1),
                        DueDate = DateTime.Parse(
                            reader.GetString(2),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind)
                    });
                }
            }

            return data;
        }
        // Adds a new book to the database using SQL Insert
        public void AddBook(Book book)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Books (item_id, title, author, genre, available)
                VALUES ($itemId, $title, $author, $genre, $available);
                """;
            command.Parameters.AddWithValue("$itemId", book.ItemID);
            command.Parameters.AddWithValue("$title", book.Title);
            command.Parameters.AddWithValue("$author", book.Author);
            command.Parameters.AddWithValue("$genre", (int)book.Genre);
            command.Parameters.AddWithValue("$available", book.Available ? 1 : 0);
            command.ExecuteNonQuery();
        }
        // Adds a new patron to the database using SQL Insert
        public void AddPatron(Patron patron)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO Patrons (patron_id, name, total_fines_cents)
                VALUES ($patronId, $name, $finesCents);
                """;
            command.Parameters.AddWithValue("$patronId", patron.GetID());
            command.Parameters.AddWithValue("$name", patron.Name);
            command.Parameters.AddWithValue("$finesCents", ToCents(patron.TotalFines));
            command.ExecuteNonQuery();
        }
        // Checks out a book by updating the book's availability
        public void CheckOutBook(Loan loan)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand updateBook = connection.CreateCommand())
            {
                updateBook.Transaction = transaction;
                updateBook.CommandText = """
                    UPDATE Books
                    SET available = 0
                    WHERE item_id = $bookId AND available = 1;
                    """;
                updateBook.Parameters.AddWithValue("$bookId", loan.BookID);
                if (updateBook.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The book is no longer available.");
                }
            }
            // Insert a new loan record into the Loans table using SQL Insert
            using (SqliteCommand insertLoan = connection.CreateCommand())
            {
                insertLoan.Transaction = transaction;
                insertLoan.CommandText = """
                    INSERT INTO Loans (book_id, patron_id, due_date)
                    VALUES ($bookId, $patronId, $dueDate);
                    """;
                insertLoan.Parameters.AddWithValue("$bookId", loan.BookID);
                insertLoan.Parameters.AddWithValue("$patronId", loan.PatronID);
                insertLoan.Parameters.AddWithValue("$dueDate", loan.DueDate.ToString("O", CultureInfo.InvariantCulture));
                insertLoan.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        // Returns a book by deleting the loan record, updating the book's availability, and adding any fines to the patron
        public void ReturnBook(Loan loan, decimal fineAmount)
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteTransaction transaction = connection.BeginTransaction();

            using (SqliteCommand deleteLoan = connection.CreateCommand())
            {
                deleteLoan.Transaction = transaction;
                deleteLoan.CommandText = """
                    DELETE FROM Loans
                    WHERE book_id = $bookId AND patron_id = $patronId;
                    """;
                deleteLoan.Parameters.AddWithValue("$bookId", loan.BookID);
                deleteLoan.Parameters.AddWithValue("$patronId", loan.PatronID);
                if (deleteLoan.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The loan record no longer exists.");
                }
            }
            // Update the book's availability to true in the Books table using SQL Update
            using (SqliteCommand updateBook = connection.CreateCommand())
            {
                updateBook.Transaction = transaction;
                updateBook.CommandText = """
                    UPDATE Books
                    SET available = 1
                    WHERE item_id = $bookId;
                    """;
                updateBook.Parameters.AddWithValue("$bookId", loan.BookID);
                if (updateBook.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The book record no longer exists.");
                }
            }
            // Update the patron's total fines in the Patrons table using SQL Update
            using (SqliteCommand updatePatron = connection.CreateCommand())
            {
                updatePatron.Transaction = transaction;
                updatePatron.CommandText = """
                    UPDATE Patrons
                    SET total_fines_cents = total_fines_cents + $fineCents
                    WHERE patron_id = $patronId;
                    """;
                updatePatron.Parameters.AddWithValue("$fineCents", ToCents(fineAmount));
                updatePatron.Parameters.AddWithValue("$patronId", loan.PatronID);
                if (updatePatron.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("The patron record no longer exists.");
                }
            }

            transaction.Commit();
        }
        // Use a SQL JOIN to retrieve active loans with book titles and patron names.
        public List<LoanSummary> GetActiveLoans()
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                SELECT Books.title, Patrons.name, Loans.due_date
                FROM Loans
                INNER JOIN Books ON Books.item_id = Loans.book_id
                INNER JOIN Patrons ON Patrons.patron_id = Loans.patron_id
                ORDER BY Loans.due_date;
                """;
            // Execute the command and read the results into a list of objects
            List<LoanSummary> loans = new();
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                loans.Add(new LoanSummary(
                    reader.GetString(0),
                    reader.GetString(1),
                    DateTime.Parse(
                        reader.GetString(2),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind)));
            }

            return loans;
        }
        // Create a SQL command to create the necessary tables if they do not exist.
        private void InitializeDatabase()
        {
            using SqliteConnection connection = OpenConnection();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Books (
                    item_id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    author TEXT NOT NULL,
                    genre INTEGER NOT NULL,
                    available INTEGER NOT NULL CHECK (available IN (0, 1))
                );

                CREATE TABLE IF NOT EXISTS Patrons (
                    patron_id TEXT PRIMARY KEY,
                    name TEXT NOT NULL,
                    total_fines_cents INTEGER NOT NULL DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS Loans (
                    book_id TEXT PRIMARY KEY,
                    patron_id TEXT NOT NULL,
                    due_date TEXT NOT NULL,
                    FOREIGN KEY (book_id) REFERENCES Books(item_id),
                    FOREIGN KEY (patron_id) REFERENCES Patrons(patron_id)
                );
                """;
            command.ExecuteNonQuery();
        }
        // Opens a new SQLite connection using the connection string.
        private SqliteConnection OpenConnection()
        {
            SqliteConnection connection = new(_connectionString);
            connection.Open();
            return connection;
        }
        private static long ToCents(decimal amount)
        {
            return decimal.ToInt64(decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));
        }
    }
}
// Represents the console-based user interface for interacting with the library management system.
namespace LibraryManagementSystem.UI
{
    public class ConsoleUI
    {
        private readonly Library _library;

        public ConsoleUI(Library library)
        {
            _library = library;
        }
        // Starts the main program loop
        public void StartProgram()
        {
            bool running = true;
            while (running)
            {
                ShowMenu();
                string choice = GetInput("Enter your choice: ");

                switch (choice)
                {
                    case "1":
                        DoViewCatalog();
                        break;
                    case "2":
                        DoCheckOutBook();
                        break;
                    case "3":
                        DoReturnBook();
                        break;
                    case "4":
                        DoViewAccountStatus();
                        break;
                    case "5":
                        DoAddPatron();
                        break;
                    case "6":
                        DoAddBook();
                        break;
                    case "7":
                        DoViewActiveLoans();
                        break;
                    case "8":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }

                if (running)
                {
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine("Action complete. Press Enter to continue...");
                    Console.ReadLine();
                }
            }

            Console.WriteLine("Thank you for using the Library Management System!");
        }
        // Displays the main menu options to the user
        private void ShowMenu()
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }

            Console.WriteLine("=== Library Management System ===");
            Console.WriteLine("1. View Book Catalog");
            Console.WriteLine("2. Check Out Book");
            Console.WriteLine("3. Return Book");
            Console.WriteLine("4. View My Account Status");
            Console.WriteLine("5. Add Patron");
            Console.WriteLine("6. Add Book");
            Console.WriteLine("7. View Active Loans (SQL JOIN)");
            Console.WriteLine("8. Quit");
        }

        private string GetInput(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine() ?? "";
        }
        // Displays the book catalog with availability status that is color coded (green for available, red for checked out)
        private void DoViewCatalog()
        {
            Console.WriteLine("\n--- Book Catalog ---");
            List<Book> catalog = _library.GetCatalog();
            if (catalog.Count == 0)
            {
                Console.WriteLine("The catalog is empty.");
                return;
            }

            foreach (Book book in catalog)
            {
                Console.ForegroundColor = book.IsAvailable() ? ConsoleColor.Green : ConsoleColor.Red;
                Console.WriteLine(book.GetDetailsString());
                Console.ResetColor();
            }
        }
        // Processes checking out a book by getting patron and book IDs.
        private void DoCheckOutBook()
        {
            Console.WriteLine("\n--- Check Out Book ---");
            string patronID = GetInput("Enter your Patron ID (e.g., p100): ");
            string bookID = GetInput("Enter the Book ID (e.g., b001): ");
            _library.CheckOutBook(patronID, bookID);
        }
        // Processes returning a book by getting patron and book IDs.
        private void DoReturnBook()
        {
            Console.WriteLine("\n--- Return Book ---");
            string patronID = GetInput("Enter your Patron ID: ");
            string bookID = GetInput("Enter the Book ID you are returning: ");
            _library.ReturnBook(patronID, bookID);
        }
        // Displays the account status of a patron, including checked-out books and total fines.
        private void DoViewAccountStatus()
        {
            Console.WriteLine("\n--- View Account Status ---");
            string patronID = GetInput("Enter your Patron ID: ");
            Patron? patron = _library.GetPatron(patronID);

            if (patron == null)
            {
                Console.WriteLine("Patron ID not found.");
                return;
            }

            Console.WriteLine(patron.GetDetailsString());
            Console.WriteLine("Checked out books:");
            List<string> bookIDs = patron.GetCheckedOutBookIDs();

            if (bookIDs.Count == 0)
            {
                Console.WriteLine("  None.");
                return;
            }
            // Display the titles of the checked-out books by looking them up in the catalog
            foreach (string bookID in bookIDs)
            {
                Book? book = _library.GetCatalog().Find(item => item.ItemID == bookID);
                Console.WriteLine($"  - {book?.Title ?? "Unknown Book"} (ID: {bookID})");
            }
        }
        // Adds a new patron to the library by getting their name and unique ID.
        private void DoAddPatron()
        {
            Console.WriteLine("\n--- Add New Patron ---");
            string name = GetInput("Enter the patron's name: ");
            string id = GetInput("Enter a unique patron ID (e.g., p123): ");

            if (_library.AddPatron(name, id))
            {
                Console.WriteLine($"Patron '{name}' (ID: {id}) added successfully.");
            }
            else
            {
                Console.WriteLine("A patron with that ID already exists. Try a different ID.");
            }
        }
        // Adds a new book to the library catalog
        private void DoAddBook()
        {
            Console.WriteLine("\n--- Add New Book ---");
            string id = GetInput("Enter the Book ID (e.g., b010): ");
            string title = GetInput("Enter the book title: ");
            string author = GetInput("Enter the author: ");

            Console.WriteLine("Genres: 0=Fiction, 1=NonFiction, 2=SciFi, 3=Fantasy, 4=Mystery");
            string genreInput = GetInput("Enter genre number: ");
            if (!int.TryParse(genreInput, out int genreValue) || genreValue < 0 || genreValue > 4)
            {
                Console.WriteLine("Invalid genre. Book not added.");
                return;
            }

            BookGenre genre = (BookGenre)genreValue;
            if (_library.AddBook(id, title, author, genre))
            {
                Console.WriteLine($"Book '{title}' (ID: {id}) added successfully.");
            }
            else
            {
                Console.WriteLine("A book with that ID already exists. Try a different ID.");
            }
        }
        // Displays a list of all active loans with book titles, patron names, and due dates.
        private void DoViewActiveLoans()
        {
            Console.WriteLine("\n--- Active Loans ---");
            List<LoanSummary> loans = _library.GetActiveLoans();
            if (loans.Count == 0)
            {
                Console.WriteLine("There are no active loans.");
                return;
            }

            foreach (LoanSummary loan in loans)
            {
                Console.WriteLine(
                    $"'{loan.BookTitle}' checked out to {loan.PatronName}, due {loan.DueDate:yyyy-MM-dd}");
            }
        }
    }
}
