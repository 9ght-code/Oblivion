#pragma once
#include <stdint.h>
#include "pe_types.h"

int PE_LoadFile(const char* path, PE_FILE* pe);
void PE_Free(PE_FILE* pe);
