StructuredBuffer<uint> _Input;
uint _InputLength;

uint GetByte(uint byteIndex)
{
    uint shift = (byteIndex % 4) * 8;
    uint byte = (_Input[byteIndex / 4] >> shift) & 0xFF; // isolate 1-byte chunks
    byte = byte - 48; // convert ASCII digits to base 10
    // 0-9 = 0-9
    // L = 28
    // R = 34
    // newline = 4294967261

    return byte;
}
