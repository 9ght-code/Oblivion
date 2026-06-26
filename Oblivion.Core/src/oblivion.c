#include "oblivion.h"
#include "pe_manager.h"
#include "pe_validator.h"
#include "oblivion_errors.h"
#include "utils.h"
#include <string.h>
#include <stdlib.h>
#include <Windows.h>
#include <winnt.h>

static void BuildResult(PE_FILE* pe, OBLIVION_RESULT* out) {
    memset(out, 0, sizeof(*out));

    /* Header */
    if (pe->is64) {
        IMAGE_NT_HEADERS64* nt64 = (IMAGE_NT_HEADERS64*)pe->nt;
        strcpy_s(out->architecture, sizeof(out->architecture), "x64");
        out->image_base          = nt64->OptionalHeader.ImageBase;
        out->entry_point         = nt64->OptionalHeader.AddressOfEntryPoint;
        out->machine             = nt64->FileHeader.Machine;
        out->characteristics     = nt64->FileHeader.Characteristics;
        out->timestamp           = nt64->FileHeader.TimeDateStamp;
        out->subsystem           = nt64->OptionalHeader.Subsystem;
        out->dll_characteristics = nt64->OptionalHeader.DllCharacteristics;
    } else {
        IMAGE_NT_HEADERS* nt = (IMAGE_NT_HEADERS*)pe->nt;
        strcpy_s(out->architecture, sizeof(out->architecture), "x86");
        out->image_base          = (uint64_t)(uint32_t)nt->OptionalHeader.ImageBase;
        out->entry_point         = nt->OptionalHeader.AddressOfEntryPoint;
        out->machine             = nt->FileHeader.Machine;
        out->characteristics     = nt->FileHeader.Characteristics;
        out->timestamp           = nt->FileHeader.TimeDateStamp;
        out->subsystem           = nt->OptionalHeader.Subsystem;
        out->dll_characteristics = nt->OptionalHeader.DllCharacteristics;
    }

    /* Sections */
    int sc = pe->sections_ptr->count;
    if (sc > OBLIVION_MAX_SECTIONS) sc = OBLIVION_MAX_SECTIONS;
    out->section_count = sc;

    for (int i = 0; i < sc; i++) {
        PE_SECTION* s  = &pe->sections_ptr->section_array_ptr[i];
        OBLIVION_SECTION* os = &out->sections[i];
        strncpy_s(os->name, sizeof(os->name), s->Name, _TRUNCATE);
        os->virtual_address = s->VirtualAdress;
        os->virtual_size    = s->VirtualSize;
        os->raw_address     = s->RawAdress;
        os->raw_size        = s->RawSize;
        os->characteristics = s->Characteristics;
        if (s->RawAdress + s->RawSize <= pe->size && s->RawSize > 0)
            os->entropy = CalcEntropy(pe->image + s->RawAdress, s->RawSize);
    }

    /* Imports — heap-allocated */
    int ic = pe->imports_ptr->count;
    if (ic > OBLIVION_MAX_IMPORTS) ic = OBLIVION_MAX_IMPORTS;
    out->import_count = ic;

    if (ic > 0) {
        out->imports = (OBLIVION_IMPORT*)calloc(ic, sizeof(OBLIVION_IMPORT));
        if (out->imports) {
            for (int i = 0; i < ic; i++) {
                PE_IMPORT* src      = &pe->imports_ptr->imports[i];
                OBLIVION_IMPORT* dst = &out->imports[i];

                if (src->dllName)
                    strncpy_s(dst->dll_name, sizeof(dst->dll_name), src->dllName, _TRUNCATE);

                int fc = src->function_count;
                if (fc > OBLIVION_MAX_FUNCTIONS) fc = OBLIVION_MAX_FUNCTIONS;
                dst->function_count = fc;

                for (int j = 0; j < fc; j++)
                    strncpy_s(dst->functions[j], OBLIVION_FUNC_NAME_LEN, src->functions[j], _TRUNCATE);
            }
        }
    }

    /* Overlay */
    uint32_t lastEnd = 0;
    for (int i = 0; i < pe->sections_ptr->count; i++) {
        PE_SECTION* s = &pe->sections_ptr->section_array_ptr[i];
        uint32_t end  = s->RawAdress + s->RawSize;
        if (end > lastEnd) lastEnd = end;
    }
    if (lastEnd > 0 && lastEnd < (uint32_t)pe->size) {
        out->overlay_offset = lastEnd;
        out->overlay_size   = (uint32_t)pe->size - lastEnd;
    }

    /* Whole-file entropy */
    out->overall_entropy = CalcEntropy(pe->image, pe->size);
}

OBLIVION_API OBLIVION_RESULT* Oblivion_AnalyzePE(const char* filepath, int* outErrorCode) {
    if (outErrorCode) *outErrorCode = PE_OK;

    PE_FILE pe;
    int result = PE_LoadFile(filepath, &pe);
    if (result != PE_OK) {
        if (outErrorCode) *outErrorCode = result;
        return NULL;
    }

    result = PE_Validate(&pe);
    if (result != PE_OK) {
        PE_Free(&pe);
        if (outErrorCode) *outErrorCode = result;
        return NULL;
    }

    OBLIVION_RESULT* out = (OBLIVION_RESULT*)calloc(1, sizeof(OBLIVION_RESULT));
    if (!out) {
        PE_Free(&pe);
        if (outErrorCode) *outErrorCode = PE_ERR_OUT_OF_MEMORY;
        return NULL;
    }

    BuildResult(&pe, out);
    PE_Free(&pe);
    return out;
}

OBLIVION_API void Oblivion_FreeResult(OBLIVION_RESULT* result) {
    if (!result) return;
    if (result->imports) free(result->imports);
    free(result);
}
