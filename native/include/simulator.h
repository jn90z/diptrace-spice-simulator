#pragma once
#if defined(_WIN32)
#define DTS_API __declspec(dllexport)
#else
#define DTS_API
#endif
extern "C" {
    DTS_API int dts_api_version();
}
