#pragma once
#include <stdint.h>
#include <Windows.h>
#include <winnt.h>
#include "oblivion.h"

#pragma pack(push, 1)
typedef struct _PE_SECTION {
    char     Name[9];
    uint32_t VirtualAdress;
    uint32_t VirtualSize;
    uint32_t RawAdress;
    uint32_t RawSize;
    uint32_t Characteristics;
} PE_SECTION;
#pragma pack(pop)

typedef struct _PE_IMPORT {
    char* dllName;
    char  (*functions)[OBLIVION_FUNC_NAME_LEN]; /* heap-allocated array of function name strings */
    int   function_count;
} PE_IMPORT;

typedef struct _PE_IMPORTS {
    PE_IMPORT* imports;
    int count;
} PE_IMPORTS;

typedef struct _PE_SECTIONS {
    PE_SECTION* section_array_ptr;
    int count;
} PE_SECTIONS;

#pragma pack(push, 8)
typedef struct _PE_FILE {
    uint8_t*    image;
    size_t      size;
    int         is64;
    void*       dos;
    void*       nt;
    PE_SECTIONS* sections_ptr;
    PE_IMPORTS*  imports_ptr;
} PE_FILE;
#pragma pack(pop)
