# Rest4GP

This library is made for share database tables/views as REST api.
It is not intended to replace other ORM frameworks, but just for share data dinamically:
you add a table/view into the database and the REST api shows it without restart or change the .NET code

It supports:
- Read data from views and/or tables (GET verb) with filter, order and pagination options
- Insert a new element (POST verb)
- Full update of an alement (PUT verb)
- Partial update of an element (PATCH verb)
- Delete of an element (DELETE verb)
- Get metadata of a single entity (table/view)
- Get metadata of the full database

For security reason, you have to specify which schema you would like to share with REST api.


Right now the library supports only Sql Server.
More provider will be added in the future.

## NuGet

To install the package run the following command on the Package Manager Console:

```
PM> Install-Package Rest4GP.SqlServer
```

## Use the library

### ASP.NET Core

In this example we will add two routes, one that works with the schema "mySchema" of the database "myDatabase"
and one that works with the schema "otherSchema" of the same database.

The first step is to configure the dependency injection:

```
        
        public void ConfigureServices(IServiceCollection services)
        {
            // This line adds the Rest4GP engine, saying it has to cache the metadata for 10 minutes
            services.AddRest4GP(options => {
                        options.MetadataCacheDelay = TimeSpan.FromMinutes(10);
                    })
                    // This line maps the "sql1" route to the first schema of the database
                    .AddSql4GP("sql1", options =>
                    {
                        options.Schema = "mySchema";
                        options.ConnectionString = "Server=localhost;Database=myDatabase;User Id=user;Password=secret";
                    });
                    // This line maps the "sql2" route to the second schema of the database
                    .AddSql4GP("sql2", options =>
                    {
                        options.Schema = "otherSchema";
                        options.ConnectionString = "Server=localhost;Database=myDatabase;User Id=user;Password=secret";
                    });
        }
```

Finally, we need to enable the Rest4GP routing.

```

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            ...

            // Enable the standard routing
            app.UseRouting();

            // Add the Rest4GP routing (before the MVC endpoints/routing)
            app.UseRest4GP();

            ...
        }
    }

```


Now you can run and check if this URLs work:

- http://localhost/sql1/$metadata => Gets the full metadata of the schema "mySchema"
- http://localhost/sql1/myTable/$metadata => Gets the metadata of the table "myTable" in the schema "mySchema"
- http://localhost/sql1/myTable => Gets all the data of the table "myTable" in the schema "mySchema"


You can change the "sql1" route with "sql2" and the library will work with the "otherSchema" of the database.


### Data result (GET)

When you ask for data (GET verb), the response is always made with two fields:
- totalCount: by default it is 0. You have to pass the "withCount" parameters in the query string to get the actual count of elements
- entities: array with the requested data


### Pagination

If you need to paginate the result (or simply get less elements), you simply need to add the "take" and "skip" query parameters.
For example, to get 10 elements after the first 5: http://localhost/sql/myTable?take=10&skip=5

Usually pagination needs also to know how many elements the table has.
For that, you need to add the withCount parameter like in this URL: http://localhost/sql/myTable?withCount=true&take=10&skip=5


### Sorting

When you need to sort data, you simply need to add the "sort" query parameters.
It's value is the JSON format that describe an array with field and direction.

For example, if you need to order a table by the "column1" field descending, your URL will be like
http://localhost/sql/myTable?sort=[{"field": "column1", "direction": "descending"}]

