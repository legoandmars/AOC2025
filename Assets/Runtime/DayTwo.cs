using System.Linq;
using Unity.Mathematics;
using UnityEngine;

// disable/enable DayTwoCompute -> L110 to toggle between part one and two
public class DayTwo : DayBase
{
    private const int ONE_ABOVE_MAX_DIGITS = 100000;
        
    [SerializeField]
    protected ComputeShader compute;

    private int length;
    private int requiredArraySize;
    private int requiredArraySizeSquare;
    private int stringArraySizeSquare;
    
    private int compareMaskPropertyId;
    private int compareMaskLengthPropertyId;
    private int compareMaskWidthPropertyId;
    private int compareMaskResultPropertyId;
    private int resultPropertyId;
    private int inputPropertyId;
    private int inputLengthPropertyId;
    private int minNumberPropertyId;
    private int maxNumberPropertyId;

    private ComputeBuffer compareMaskBuffer;
    private ComputeBuffer minNumberBuffer;
    private ComputeBuffer maxNumberBuffer;
    private ComputeBuffer compareMaskResultBuffer;
    private ComputeBuffer resultBuffer;
    
    public override void Setup()
    {
        requiredArraySize = (int)GetArrayStartIndex(ONE_ABOVE_MAX_DIGITS, 5, 1);
        requiredArraySizeSquare = 102400 / 32; // don't wanna autocalc this, should be fine as long as largest number length remains at 10-11
        stringArraySizeSquare = Mathf.CeilToInt(inputLength / 32f);
        
        compareMaskBuffer = new ComputeBuffer(requiredArraySize, sizeof(ulong));
        minNumberBuffer = new ComputeBuffer(stringArraySizeSquare * 32, sizeof(ulong));
        maxNumberBuffer = new ComputeBuffer(stringArraySizeSquare * 32, sizeof(ulong));
        compareMaskResultBuffer = new ComputeBuffer(requiredArraySizeSquare * 32 * 32, sizeof(uint));
        resultBuffer = new ComputeBuffer(1, sizeof(ulong));
        
        compareMaskPropertyId = Shader.PropertyToID("_CompareMasks");
        compareMaskLengthPropertyId = Shader.PropertyToID("_CompareMasksArraySize");
        compareMaskWidthPropertyId = Shader.PropertyToID("_CompareMasksWidth");
        compareMaskResultPropertyId = Shader.PropertyToID("_CompareMaskResult");
        resultPropertyId = Shader.PropertyToID("_Result");
        inputPropertyId = Shader.PropertyToID("_Input");
        inputLengthPropertyId = Shader.PropertyToID("_InputLength");
        minNumberPropertyId = Shader.PropertyToID("_MinNumbers");
        maxNumberPropertyId = Shader.PropertyToID("_MaxNumbers");
    }

    public override void RunCode()
    {
        // clear buffer
        compute.SetInt(compareMaskLengthPropertyId, requiredArraySize);
        compute.SetInt(compareMaskWidthPropertyId, requiredArraySizeSquare);
        compute.SetBuffer(1, compareMaskPropertyId, compareMaskBuffer);
        compute.SetBuffer(1, compareMaskResultPropertyId, compareMaskResultBuffer);
        compute.SetBuffer(1, minNumberPropertyId, minNumberBuffer);
        compute.SetBuffer(1, maxNumberPropertyId, maxNumberBuffer);
        compute.SetBuffer(1, resultPropertyId, resultBuffer);
        compute.Dispatch(1, requiredArraySizeSquare, 1, 1); 

        // generate compare mask with every possible invalid ID
        // in a perfect world we would analyze the exact dimenions of the numbers and only generate mask parts we need
        // however, because of the wonders of technology, we can just throw more compute at it
        compute.SetBuffer(0, compareMaskPropertyId, compareMaskBuffer);
        compute.Dispatch(0, 100000 / 32, 1, 1); // only needs 100,000 dispatch since we're running through first 100k numbers
        
        // using [-] as a delimiter, build an array both forwards and backwards
        compute.SetInt(inputLengthPropertyId, inputLength);        
        compute.SetBuffer(2, inputPropertyId,  inputBuffer);
        compute.SetBuffer(3, inputPropertyId,  inputBuffer);
        compute.SetBuffer(2, minNumberPropertyId, minNumberBuffer);
        compute.SetBuffer(3, maxNumberPropertyId, maxNumberBuffer);
        compute.Dispatch(2,  stringArraySizeSquare, 1, 1);
        compute.Dispatch(3,  stringArraySizeSquare, 1, 1);
        
        // we have all the data arrays we need - compare the max/min numbers against the huge mask array
        compute.SetBuffer(4, compareMaskPropertyId, compareMaskBuffer);
        compute.SetBuffer(4, minNumberPropertyId, minNumberBuffer);
        compute.SetBuffer(4, compareMaskResultPropertyId, compareMaskResultBuffer);
        compute.SetBuffer(4, maxNumberPropertyId, maxNumberBuffer);
        compute.SetBuffer(4, resultPropertyId, resultBuffer);
        compute.Dispatch(4, requiredArraySizeSquare, 1, 1);
        
        // the exact number mask collisions are now calculated, but we're delaying actually adding them up into the final pass
        // this is because having a bunch of interlocked adds in every thread of the previous method would greatly slow things down
        compute.SetBuffer(5, compareMaskPropertyId, compareMaskBuffer);
        compute.SetBuffer(5, compareMaskResultPropertyId, compareMaskResultBuffer);
        compute.SetBuffer(5, resultPropertyId, resultBuffer);
        compute.Dispatch(5, requiredArraySizeSquare, 1, 1);
    }

    public override void Cleanup()
    {
        var bytes = new ulong[1];
        resultBuffer.GetData(bytes);
        Debug.Log("Result: "+bytes[0]);

        compareMaskBuffer.Release();
        minNumberBuffer.Release();
        maxNumberBuffer.Release();
        compareMaskResultBuffer.Release();
        resultBuffer.Release();
    }

    public override void GetData()
    {
        var bytes = new ulong[1];
        resultBuffer.GetData(bytes);
    }
    
    // pow(10, x) or even round(pow(10, x)) does not work for 7
    // this is because there's no way to do integer powers, only floats
    // so this is a drop-in method for int pow
    uint PowerOfTen(uint count)
    {
        uint value = 1;
    
        // this is immensely stupid and probably not the best way to do this
        // branching would probably be cheaper
        // however, you do have to admit it has swag
        value *= ((count & 1) * 9) + 1;
        value *= (uint)((count & 2) * 99/2.0f) + 1;
        value *= (uint)((count & 4) * 9999/4.0f) + 1;
        value *= (uint)((count & 8) * 99999999/8.0f) + 1;
    
        return value;
        //return round(pow(10, count));
    }

    uint GetFactorCount(uint length)
    {
        // hack - this depends on (1 << -1) resolving as 0
        // halve via bitshift, resolves to 9, 4, 2, 1, 1 (amount of "valid" digit lengths for palindromes)
        // it might be possible to refit this on numbers greater than 5 digits, but right now it would break horribly. so hopefully that's not a thing.
        return (uint)math.clamp(9 / (1 << (int)length), 1, 9);
    }

    // used to get total required array size
    // i am trying to run as little code on CPU as possible, but i do unfortunately need to get the dispatch size
    // technically we could do this on GPU but the GPU -> CPU data time would destroy performance
    uint GetArrayStartIndex(uint index, uint length, uint factorCount)
    {
        // first, add the start indexes of previous "tens groups"    
        uint startIndex = 0;
        for (uint i = 0; i < length; i++)
        {
            startIndex += (9 * PowerOfTen(i) * GetFactorCount(i)); // what
        }
    
        // finally, add the previous numbers from this "tens group"
        uint previousNumberCount = (index - PowerOfTen(length)) * factorCount;
        startIndex += previousNumberCount;
    
        return startIndex;
    }
}