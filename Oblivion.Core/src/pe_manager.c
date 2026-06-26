#include "pe_manager.h"
#include "oblivion_errors.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

int PE_LoadFile(const char* path, PE_FILE* pe) {

	if (!path)
		return PE_ERR_FILE_NOT_FOUND;

	memset(pe, 0, sizeof(*pe));

	FILE* file = fopen(path, "rb");

	if (!file)
		return PE_ERR_FILE_NOT_FOUND;

	fseek(file, 0, SEEK_END);
	pe->size = ftell(file);
	fseek(file, 0, SEEK_SET);

	pe->image = (uint8_t*)malloc(pe->size);
	if (!pe->image)
	{
		fclose(file);
		return PE_ERR_OUT_OF_MEMORY;
	}

	if (fread(pe->image, 1, pe->size, file) != pe->size)
	{
		fclose(file);
		free(pe->image);
		pe->image = NULL;
		return PE_ERR_READ_FAILED;
	}

	fclose(file);
	return PE_OK;

}

void PE_Free(PE_FILE* pe) {
	if (!pe)
		return;

	if (pe->image) {
		free(pe->image);
	}

	if (pe->sections_ptr) {
		free(pe->sections_ptr->section_array_ptr);
		free(pe->sections_ptr);
	}

	if (pe->imports_ptr) {
		if (pe->imports_ptr->imports) {
			for (int i = 0; i < pe->imports_ptr->count; i++) {
				if (pe->imports_ptr->imports[i].functions)
					free(pe->imports_ptr->imports[i].functions);
			}
			free(pe->imports_ptr->imports);
		}
		free(pe->imports_ptr);
	}

	memset(pe, 0, sizeof(*pe));
}
