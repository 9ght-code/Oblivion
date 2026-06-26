#pragma once
#include <stdint.h>
#include <stdlib.h>

#ifdef _WIN32
#define OBLIVION_API __declspec(dllexport)
#else
#define OBLIVION_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

#define OBLIVION_MAX_SECTIONS  96
#define OBLIVION_MAX_IMPORTS   128
#define OBLIVION_MAX_FUNCTIONS 512
#define OBLIVION_FUNC_NAME_LEN 64
#define OBLIVION_DLL_NAME_LEN  128

typedef struct {
    char     dll_name[OBLIVION_DLL_NAME_LEN];
    char     functions[OBLIVION_MAX_FUNCTIONS][OBLIVION_FUNC_NAME_LEN];
    int      function_count;
} OBLIVION_IMPORT;

typedef struct {
    char     name[9];
    uint32_t virtual_address;
    uint32_t virtual_size;
    uint32_t raw_address;
    uint32_t raw_size;
    uint32_t characteristics;
    double   entropy;
} OBLIVION_SECTION;

typedef struct {
    /* Header fields */
    char     architecture[8];
    uint64_t image_base;
    uint32_t entry_point;
    uint16_t machine;
    uint16_t characteristics;
    uint32_t timestamp;
    uint16_t subsystem;
    uint16_t dll_characteristics;

    /* Sections */
    OBLIVION_SECTION sections[OBLIVION_MAX_SECTIONS];
    int              section_count;

    /* Imports — heap-allocated array, length = import_count */
    OBLIVION_IMPORT* imports;
    int              import_count;

    /* Overlay */
    uint32_t overlay_offset;
    uint32_t overlay_size;

    /* Whole-file entropy */
    double   overall_entropy;
} OBLIVION_RESULT;

/* Returns heap-allocated OBLIVION_RESULT* on success, NULL on failure.
   Caller must free with Oblivion_FreeResult. */
OBLIVION_API OBLIVION_RESULT* Oblivion_AnalyzePE(const char* filePath, int* outErrorCode);

/* Free result returned by Oblivion_AnalyzePE. */
OBLIVION_API void Oblivion_FreeResult(OBLIVION_RESULT* result);

#ifdef __cplusplus
}
#endif
