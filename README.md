# Library Books API

A small ASP.NET Core (.NET 10) REST API for managing the books in a library store, backed by SQLite.

## Run

```bash
dotnet run --project src/LibraryBooks.Api
```

The database is created on first start at `library.db`. Set `ConnectionStrings__Library` to change it, and `PORT` to choose the port.

## Endpoints

| Method | Path | Description |
| --- | --- | --- |
| GET | `/health` | Health check |
| GET | `/books?search=` | List books, optionally filtered by title or author |
| GET | `/books/{id}` | Get a book |
| POST | `/books` | Add a book |
| PUT | `/books/{id}` | Update a book |
| DELETE | `/books/{id}` | Remove a book |

Example:

```bash
curl -X POST localhost:5000/books \
  -H 'Content-Type: application/json' \
  -d '{"title":"Things Fall Apart","author":"Chinua Achebe","isbn":"9780385474542","publishedYear":1958,"copiesAvailable":3}'
```

Invalid input returns `400` with field errors, and a duplicate ISBN returns `409`.

## Test

```bash
dotnet test
```

The tests start the API in memory against a fresh SQLite file and exercise every endpoint over HTTP.
