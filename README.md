# REST gRPC Client

---

This project was built using .NET Core 5

To run the project:

```bash
dotnet Grpc.Client.dll
```

Make sure the gRPC server is running and their URL(s) are listed in `appsettings.json` under __GrpcServers__

---

Endpoint information is available as Swagger docs.
The multiplication modes available are:

- **SingleServerSingleMatrix** Matrices will be multiplied in a single server and sent as a single matrix (no divide and conquer from client).
- **SingleServerSubMatrices** Matrices will be divided in the client and sent to the server. The minimum sub-matrix size can be set as a query param.
- **MultipleSevers** Matrices will be divided and the load will be split among all available servers.
- **MultipleServersFootprint** In addition to splitting the load among servers, the number of servers will be calculated based on a footprint. The deadline can be set as a query param.
- **SingleServerMultiThread** The matrices will be sent to a single server which will multiply them in multiple threads.

