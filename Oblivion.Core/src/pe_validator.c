#include "pe_validator.h"
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

/* Parse function names for one import descriptor into pe_import->functions */
static void ParseImportFunctions32(PE_FILE* pe, PIMAGE_NT_HEADERS nt, PIMAGE_IMPORT_DESCRIPTOR desc, PE_IMPORT* imp) {
    imp->functions      = NULL;
    imp->function_count = 0;

    DWORD thunkRva = desc->OriginalFirstThunk != 0 ? desc->OriginalFirstThunk : desc->FirstThunk;
    if (thunkRva == 0) return;

    DWORD thunkOffset = RvaToOffset(nt, thunkRva);
    if (thunkOffset == 0 || thunkOffset >= (DWORD)pe->size) return;

    /* Count thunks first */
    DWORD* thunk = (DWORD*)(pe->image + thunkOffset);
    int count = 0;
    while ((uint8_t*)(thunk + 1) <= pe->image + pe->size && *thunk != 0 && count < OBLIVION_MAX_FUNCTIONS) {
        count++;
        thunk++;
    }
    if (count == 0) return;

    imp->functions = (char(*)[OBLIVION_FUNC_NAME_LEN])malloc(count * OBLIVION_FUNC_NAME_LEN);
    if (!imp->functions) return;

    thunk = (DWORD*)(pe->image + thunkOffset);
    int idx = 0;
    while ((uint8_t*)(thunk + 1) <= pe->image + pe->size && *thunk != 0 && idx < count) {
        DWORD entry = *thunk;

        if (entry & 0x80000000) {
            /* Import by ordinal */
            _snprintf_s(imp->functions[idx], OBLIVION_FUNC_NAME_LEN, _TRUNCATE, "Ordinal #%u", entry & 0xFFFF);
        } else {
            /* Import by name: hint (2 bytes) + name */
            DWORD hintOffset = RvaToOffset(nt, entry & 0x7FFFFFFF);
            if (hintOffset == 0 || hintOffset + 2 >= pe->size) {
                imp->functions[idx][0] = '\0';
            } else {
                const char* name = (const char*)(pe->image + hintOffset + 2);
                size_t maxLen = pe->size - (hintOffset + 2);
                if (maxLen >= OBLIVION_FUNC_NAME_LEN) maxLen = OBLIVION_FUNC_NAME_LEN - 1;
                strncpy_s(imp->functions[idx], OBLIVION_FUNC_NAME_LEN, name, maxLen);
                imp->functions[idx][OBLIVION_FUNC_NAME_LEN - 1] = '\0';
            }
        }
        idx++;
        thunk++;
    }
    imp->function_count = idx;
}

static void ParseImportFunctions64(PE_FILE* pe, PIMAGE_NT_HEADERS64 nt64, PIMAGE_IMPORT_DESCRIPTOR desc, PE_IMPORT* imp) {
    imp->functions      = NULL;
    imp->function_count = 0;

    DWORD thunkRva = desc->OriginalFirstThunk != 0 ? desc->OriginalFirstThunk : desc->FirstThunk;
    if (thunkRva == 0) return;

    DWORD thunkOffset = RvaToOffset64(nt64, thunkRva);
    if (thunkOffset == 0 || thunkOffset >= (DWORD)pe->size) return;

    ULONGLONG* thunk = (ULONGLONG*)(pe->image + thunkOffset);
    int count = 0;
    while ((uint8_t*)(thunk + 1) <= pe->image + pe->size && *thunk != 0 && count < OBLIVION_MAX_FUNCTIONS) {
        count++;
        thunk++;
    }
    if (count == 0) return;

    imp->functions = (char(*)[OBLIVION_FUNC_NAME_LEN])malloc(count * OBLIVION_FUNC_NAME_LEN);
    if (!imp->functions) return;

    thunk = (ULONGLONG*)(pe->image + thunkOffset);
    int idx = 0;
    while ((uint8_t*)(thunk + 1) <= pe->image + pe->size && *thunk != 0 && idx < count) {
        ULONGLONG entry = *thunk;

        if (entry & 0x8000000000000000ULL) {
            _snprintf_s(imp->functions[idx], OBLIVION_FUNC_NAME_LEN, _TRUNCATE, "Ordinal #%u", (unsigned)(entry & 0xFFFF));
        } else {
            DWORD hintOffset = RvaToOffset64(nt64, (DWORD)(entry & 0x7FFFFFFF));
            if (hintOffset == 0 || hintOffset + 2 >= pe->size) {
                imp->functions[idx][0] = '\0';
            } else {
                const char* name = (const char*)(pe->image + hintOffset + 2);
                size_t maxLen = pe->size - (hintOffset + 2);
                if (maxLen >= OBLIVION_FUNC_NAME_LEN) maxLen = OBLIVION_FUNC_NAME_LEN - 1;
                strncpy_s(imp->functions[idx], OBLIVION_FUNC_NAME_LEN, name, maxLen);
                imp->functions[idx][OBLIVION_FUNC_NAME_LEN - 1] = '\0';
            }
        }
        idx++;
        thunk++;
    }
    imp->function_count = idx;
}

static int PE_ParseImports32(PE_FILE* pe, PIMAGE_NT_HEADERS nt, PE_IMPORT** outImports, int* outCount) {
    if (nt->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_IMPORT) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    IMAGE_DATA_DIRECTORY importDirectory = nt->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDirectory.Size == 0 || importDirectory.VirtualAddress == 0) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    DWORD importOffset = RvaToOffset(nt, importDirectory.VirtualAddress);
    if (importOffset == 0 || importOffset + sizeof(IMAGE_IMPORT_DESCRIPTOR) > pe->size) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    PIMAGE_IMPORT_DESCRIPTOR importDesc = (PIMAGE_IMPORT_DESCRIPTOR)(pe->image + importOffset);

    int importCount = 0;
    PIMAGE_IMPORT_DESCRIPTOR counter = importDesc;
    while (counter->Name != 0) {
        DWORD entryEnd = (DWORD)((uint8_t*)(counter + 1) - pe->image);
        if (entryEnd > pe->size) break;
        importCount++;
        counter++;
    }

    if (importCount == 0) { *outImports = NULL; *outCount = 0; return PE_OK; }

    PE_IMPORT* imp = (PE_IMPORT*)calloc(importCount, sizeof(PE_IMPORT));
    if (!imp) return PE_ERR_OUT_OF_MEMORY;

    for (int i = 0; i < importCount; i++) {
        DWORD nameOffset = RvaToOffset(nt, importDesc->Name);
        imp[i].dllName = (nameOffset != 0 && nameOffset < pe->size)
            ? (char*)(pe->image + nameOffset)
            : NULL;

        ParseImportFunctions32(pe, nt, importDesc, &imp[i]);
        importDesc++;
    }

    *outImports = imp;
    *outCount   = importCount;
    return PE_OK;
}

static int PE_ParseImports64(PE_FILE* pe, PIMAGE_NT_HEADERS64 nt64, PE_IMPORT** outImports, int* outCount) {
    if (nt64->OptionalHeader.NumberOfRvaAndSizes <= IMAGE_DIRECTORY_ENTRY_IMPORT) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    IMAGE_DATA_DIRECTORY importDirectory = nt64->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDirectory.Size == 0 || importDirectory.VirtualAddress == 0) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    DWORD importOffset = RvaToOffset64(nt64, importDirectory.VirtualAddress);
    if (importOffset == 0 || importOffset + sizeof(IMAGE_IMPORT_DESCRIPTOR) > pe->size) {
        *outImports = NULL; *outCount = 0; return PE_OK;
    }

    PIMAGE_IMPORT_DESCRIPTOR importDesc = (PIMAGE_IMPORT_DESCRIPTOR)(pe->image + importOffset);

    int importCount = 0;
    PIMAGE_IMPORT_DESCRIPTOR counter = importDesc;
    while (counter->Name != 0) {
        DWORD entryEnd = (DWORD)((uint8_t*)(counter + 1) - pe->image);
        if (entryEnd > pe->size) break;
        importCount++;
        counter++;
    }

    if (importCount == 0) { *outImports = NULL; *outCount = 0; return PE_OK; }

    PE_IMPORT* imp = (PE_IMPORT*)calloc(importCount, sizeof(PE_IMPORT));
    if (!imp) return PE_ERR_OUT_OF_MEMORY;

    for (int i = 0; i < importCount; i++) {
        DWORD nameOffset = RvaToOffset64(nt64, importDesc->Name);
        imp[i].dllName = (nameOffset != 0 && nameOffset < pe->size)
            ? (char*)(pe->image + nameOffset)
            : NULL;

        ParseImportFunctions64(pe, nt64, importDesc, &imp[i]);
        importDesc++;
    }

    *outImports = imp;
    *outCount   = importCount;
    return PE_OK;
}

int PE_Validate(PE_FILE* pe) {

    if (pe->size < sizeof(IMAGE_DOS_HEADER))
        return PE_ERR_INVALID_MZ;

    IMAGE_DOS_HEADER* dos = (IMAGE_DOS_HEADER*)pe->image;
    if (dos->e_magic != IMAGE_MZ_SIGNATURE)
        return PE_ERR_INVALID_MZ;

    if (dos->e_lfanew < 0 ||
        (size_t)dos->e_lfanew + sizeof(DWORD) + sizeof(IMAGE_FILE_HEADER) > pe->size)
        return PE_ERR_INVALID_PE;

    IMAGE_NT_HEADERS* nt = (IMAGE_NT_HEADERS*)(pe->image + dos->e_lfanew);
    if (nt->Signature != IMAGE_NT_SIGNATURE)
        return PE_ERR_INVALID_PE;

    if (nt->FileHeader.Machine == IMAGE_FILE_MACHINE_AMD64)
        pe->is64 = 1;
    else if (nt->FileHeader.Machine == IMAGE_FILE_MACHINE_I386)
        pe->is64 = 0;
    else
        return PE_ERR_UNSUPPORTED_ARCH;

    if (pe->is64) {
        if ((size_t)dos->e_lfanew + sizeof(IMAGE_NT_HEADERS64) > pe->size)
            return PE_ERR_INVALID_PE;
    } else {
        if ((size_t)dos->e_lfanew + sizeof(IMAGE_NT_HEADERS) > pe->size)
            return PE_ERR_INVALID_PE;
    }

    IMAGE_SECTION_HEADER* src;
    if (pe->is64) {
        src = (IMAGE_SECTION_HEADER*)((uint8_t*)nt +
            sizeof(DWORD) + sizeof(IMAGE_FILE_HEADER) + nt->FileHeader.SizeOfOptionalHeader);
    } else {
        src = IMAGE_FIRST_SECTION(nt);
    }

    int count = nt->FileHeader.NumberOfSections;
    if (count == 0) return PE_ERR_INVALID_SECTION;

    size_t sectionsEnd = (size_t)((uint8_t*)(src + count) - pe->image);
    if (sectionsEnd > pe->size) return PE_ERR_INVALID_SECTION;

    PE_SECTION* arr = (PE_SECTION*)malloc(sizeof(PE_SECTION) * count);
    if (!arr) return PE_ERR_OUT_OF_MEMORY;

    for (int i = 0; i < count; i++) {
        memset(&arr[i], 0, sizeof(PE_SECTION));
        memcpy(arr[i].Name, src[i].Name, 8);
        arr[i].Name[8]           = '\0';
        arr[i].VirtualAdress     = src[i].VirtualAddress;
        arr[i].VirtualSize       = src[i].Misc.VirtualSize;
        arr[i].RawAdress         = src[i].PointerToRawData;
        arr[i].RawSize           = src[i].SizeOfRawData;
        arr[i].Characteristics   = src[i].Characteristics;
    }

    PE_SECTIONS* sections = (PE_SECTIONS*)malloc(sizeof(PE_SECTIONS));
    if (!sections) { free(arr); return PE_ERR_OUT_OF_MEMORY; }

    sections->count              = count;
    sections->section_array_ptr  = arr;

    PE_IMPORT* importArr  = NULL;
    int        importCount = 0;
    int        importResult;

    if (pe->is64) {
        IMAGE_NT_HEADERS64* nt64 = (IMAGE_NT_HEADERS64*)(pe->image + dos->e_lfanew);
        importResult = PE_ParseImports64(pe, nt64, &importArr, &importCount);
    } else {
        importResult = PE_ParseImports32(pe, nt, &importArr, &importCount);
    }

    if (importResult != PE_OK) {
        free(arr);
        free(sections);
        return importResult;
    }

    PE_IMPORTS* imports = (PE_IMPORTS*)malloc(sizeof(PE_IMPORTS));
    if (!imports) {
        free(arr);
        free(sections);
        if (importArr) free(importArr);
        return PE_ERR_OUT_OF_MEMORY;
    }

    imports->imports = importArr;
    imports->count   = importCount;

    pe->dos          = dos;
    pe->nt           = (void*)(pe->image + dos->e_lfanew);
    pe->sections_ptr = sections;
    pe->imports_ptr  = imports;

    return PE_OK;
}
