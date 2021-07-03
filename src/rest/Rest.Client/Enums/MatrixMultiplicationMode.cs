namespace Client.Enums
{
    /// <summary>
    /// Multiplication modes supported.
    /// <see cref="SingleServerSingleMatrix"/>: Calls a single server and sends the entire matrix over the network.
    /// <see cref="SingleServerSubMatrices"/>: Calls a single server but following a divide and conquer approach from the client.
    /// Therefore, the matrix is broken down before sending it.
    /// <see cref="MultipleSevers"/>: Calls all servers available and sends parts of the divided matrix to each in a Round Robin fashion.
    /// <see cref="MultipleServersFootprint"/>: Given a deadline, the client will try to guess how many servers will be needed
    /// (based on a footprint) to perform the multiplication within the deadline.
    /// <see cref="SingleServerMultiThread"/>: Using a divide and conquer approach but calling a single server where the
    /// multiplication is performed in parallel, using multiple cores if available.
    /// </summary>
    public enum MatrixMultiplicationMode
    {
        SingleServerSingleMatrix,
        SingleServerSubMatrices,
        MultipleSevers,
        MultipleServersFootprint,
        SingleServerMultiThread
    }
}