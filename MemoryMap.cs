using System;

public class MemoryMap
{
    public const ushort BASE = 0x0000;
    public const ushort ROM_BASE = BASE;
    public const ushort ROM_TOP = 0x07ff;
    public const ushort RAM_BASE = 0x8000;
    public const ushort RAM_TOP = RAM_BASE + 0xff;
    public const ushort TEMPORARY_RESULT_STORAGE_RAM = RAM_TOP + 0x0001;//8100h
    public const ushort TEMPORARY_RESULT_STORAGE_RAM_TOP = TEMPORARY_RESULT_STORAGE_RAM + 0x00FF;//81ffh

}
/*
 * 
 * 0x0000
 * .
 * ROM
 * .
 * 0x07ff
 * 0x8000
 * .
 * .
 * RAM
 * .
 * .
 * 0x80ff
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */