#pragma once
#include "oblivion_errors.h"
#include "pe_types.h"
#include "utils.h"

#define IMAGE_MZ_SIGNATURE 0x5A4D
#define E_LFANEW_OFFSET 0x3C

int PE_Validate(PE_FILE* pe);