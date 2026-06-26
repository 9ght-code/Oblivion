#include "utils.h"
#include <math.h>

DWORD RvaToOffset(PIMAGE_NT_HEADERS ntHeaders, DWORD rva) {
    PIMAGE_SECTION_HEADER section = IMAGE_FIRST_SECTION(ntHeaders);
    WORD numberOfSections = ntHeaders->FileHeader.NumberOfSections;

    for (WORD i = 0; i < numberOfSections; i++) {
        DWORD sectionStart = section->VirtualAddress;
        DWORD sectionSize  = section->Misc.VirtualSize;

        if (sectionSize == 0)
            sectionSize = section->SizeOfRawData;

        if (sectionSize > 0 && rva >= sectionStart && rva < sectionStart + sectionSize) {
            return (rva - sectionStart) + section->PointerToRawData;
        }
        section++;
    }
    return 0;
}

DWORD RvaToOffset64(PIMAGE_NT_HEADERS64 ntHeaders, DWORD rva) {
    PIMAGE_SECTION_HEADER section = (PIMAGE_SECTION_HEADER)(
        (uint8_t*)ntHeaders +
        sizeof(DWORD) +
        sizeof(IMAGE_FILE_HEADER) +
        ntHeaders->FileHeader.SizeOfOptionalHeader
    );
    WORD numberOfSections = ntHeaders->FileHeader.NumberOfSections;

    for (WORD i = 0; i < numberOfSections; i++) {
        DWORD sectionStart = section->VirtualAddress;
        DWORD sectionSize  = section->Misc.VirtualSize;

        if (sectionSize == 0)
            sectionSize = section->SizeOfRawData;

        if (sectionSize > 0 && rva >= sectionStart && rva < sectionStart + sectionSize) {
            return (rva - sectionStart) + section->PointerToRawData;
        }
        section++;
    }
    return 0;
}

double CalcEntropy(const uint8_t* data, size_t size) {
    if (!data || size == 0)
        return 0.0;

    size_t freq[256] = { 0 };
    for (size_t i = 0; i < size; i++)
        freq[data[i]]++;

    double entropy = 0.0;
    for (int i = 0; i < 256; i++) {
        if (freq[i] == 0) continue;
        double p = (double)freq[i] / (double)size;
        entropy -= p * log2(p);
    }
    return entropy;
}
