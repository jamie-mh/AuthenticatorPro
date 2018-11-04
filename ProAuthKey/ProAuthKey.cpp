#include <cstring>
#include <string>
#include "md5.h"

extern "C" {
    char *get_key()
	{
        std::string key = md5("vyp7aQ0VpEVZqpUl75HK86TMJmRMgiB0sGuou8xR");

        char *heap_key = static_cast<char *>(malloc(sizeof(char) * (40 + 1)));
        strcpy(heap_key, key.c_str());

        return heap_key;
	}
}
