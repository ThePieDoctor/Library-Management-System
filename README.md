# Overview

This library management system stores its book catalog, patrons, and active loans in a SQLite relational database. The program supports viewing the book catalog, checking out and returning books, viewing a patron's account, and adding books and patrons. The active-loans screen runs a SQL JOIN to display each loan with its book title and patron name.

The program creates library.db the first time it runs and creates its tables automatically from the seeded json file data. A starter catalog and patrons are inserted when the database is empty. The previous library_data.json file is not edited but SQLite data is stored separately in library.db.

The purpose of this project is to practice designing a database that uses SQL to store the data in a more efficient and structured way so it can be more easily edited and called upon within the code.

[Software Demo Video](https://youtu.be/GRGe4zzg45k)

# Relational Database

The program uses SQLite through the Microsoft Data Sqlite library. The database uses a local file named library.db.

- Books stores each book's ID, title, author, genre, and availability. item_id is the primary key.
- Patrons stores each patron's ID, name, and accumulated fines in cents. patron_id is the primary key.
- Loans stores active loans. book_id is the primary key and a foreign key to Books; patron_id is a foreign key to Patrons. The due date is stored as an ISO 8601 text value.

Adding books and patrons uses SQL INSERT statements. Checkout updates book availability and inserts a loan in one transaction. Return deletes the loan and updates book availability and patron fines in one transaction. The catalog and accounts are loaded with SELECT queries, and the active-loans screen demonstrates a JOIN.

# Development Environment

- Visual Studio Code 
- C# 5.12.0-1.26452.1 x64 Bit
- SQLite
- Github

# Useful Websites

- [W3 Schools](https://www.w3schools.com/cpp/default.asp)
- [Stack Overflow](https://stackoverflow.com/)
- [Markdown Guide](https://www.markdownguide.org/cheat-sheet/)
- [SQLite Documentation](https://www.sqlite.org/docs.html)
- [Microsoft.Data.Sqlite Documentation](https://learn.microsoft.com/dotnet/standard/data/sqlite/)
- [SQL Tutorial: SQLite INNER JOIN](https://www.sqlitetutorial.net/sqlite-inner-join/)


# Future Work

- Add a way to edit and remove catalog entries and patrons safely.
- Find a place to store loan history instead of deleting a loan when a book is returned.
- Add a book search option for finding a book in a long catalog.
