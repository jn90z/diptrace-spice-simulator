# Native engine boundary

The native engine is a C++20 DLL built with CMake. The public ABI uses C linkage and fixed-width interoperable types; do not expose STL types or C++ exceptions across the boundary.

Initial export: `dts_api_version()` returns 1. This is a smoke test, **not** a simulation.

Future work: opaque solver handles, structured error codes, caller-owned buffers, deterministic lifetime management, sparse DC solver benchmarks and ngspice integration after license review.

The WPF application and C++ DLL currently build independently. P/Invoke wiring and DLL packaging are planned.
