#pragma once
#include <stdint.h>
#include "pe_types.h"

DWORD  RvaToOffset(PIMAGE_NT_HEADERS ntHeaders, DWORD rva);
DWORD  RvaToOffset64(PIMAGE_NT_HEADERS64 ntHeaders, DWORD rva);
double CalcEntropy(const uint8_t* data, size_t size);
