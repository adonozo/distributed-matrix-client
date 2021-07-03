# REST gRPC Client

This is the client for a distributed matrix multiplication service. Matrices are multiplied using the "divide and conquer" approach, 
but all the multiplication takes place in the server(s). There is a mixture of technologies and techniques used. Matrices are uploaded 
via the **REST** interface, as plain text files. Matrices are sent over the network to the multiplication servers with **gRPC**.  To improve efficiency, files are read and returned as streams, gRPC calls are asynchronous, and matrices operations are executed in parallel.

Multiplication modes available:

- **SingleServerSingleMatrix** Matrices will be multiplied in a single server and sent as a single matrix (no divide and conquer from client).
- **SingleServerSubMatrices** Matrices will be divided in the client and sent to the server. The minimum sub-matrix size can be set as a query param.
- **MultipleSevers** Matrices will be divided and the load will be split among all available servers.
- **MultipleServersFootprint** In addition to splitting the load among servers, the number of servers will be calculated based on a footprint. The deadline can be set as a query param.
- **SingleServerMultiThread** The matrices will be sent to a single server which will multiply them in multiple threads.

---

This project was built using .NET Core 5

To run the project:

```bash
dotnet Grpc.Client.dll
```

Make sure the gRPC server is running and their URL(s) are listed in `appsettings.json` under __GrpcServers__

---

Endpoint information is available as Swagger docs.