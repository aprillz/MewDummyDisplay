// Every test here is pure computation with no shared state, so they run in parallel.
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
