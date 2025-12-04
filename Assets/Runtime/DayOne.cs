using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DayOne : DayBase
{
    // not a huge fan of this hardcoding, but it should give us enough room for any input (probably)
    private const int THREAD_X = 32;
    private const int THREAD_Y = 32;
    private const int THREAD_Z = 24;
    private const int THREAD_SIZE = THREAD_X * THREAD_Y * THREAD_Z;
    
    [SerializeField]
    protected ComputeShader compute;

    private int length;
    private int inputPropertyId;
    private int inputLengthPropertyId;
    private int resultPropertyId;
    private int deltasPropertyId;
    private int deltasBPropertyId;
    private int stridePropertyId;

    private ComputeBuffer resultBuffer;
    private ComputeBuffer deltasBuffer;
    private ComputeBuffer deltasBufferB;
    
    public override void Setup()
    {
        resultBuffer = new ComputeBuffer(1, 4);
        resultBuffer.SetData(new int[]{0});
        deltasBuffer = new ComputeBuffer(THREAD_SIZE, 4); // don't love this being so large, but it prevents us from needing to do extra passes/sorting...
        deltasBufferB = new ComputeBuffer(THREAD_SIZE, 4);
        
        inputPropertyId = Shader.PropertyToID("_Input");
        inputLengthPropertyId = Shader.PropertyToID("_InputLength");
        resultPropertyId = Shader.PropertyToID("_Result");
        deltasPropertyId = Shader.PropertyToID("_Deltas");
        deltasBPropertyId = Shader.PropertyToID("_DeltasB");
        stridePropertyId = Shader.PropertyToID("_Stride");
    }

    public override void RunCode()
    {
        compute.SetInt(inputLengthPropertyId, inputLength);        

        compute.SetBuffer(0, inputPropertyId,  inputBuffer);
        compute.SetBuffer(0, deltasPropertyId,  deltasBuffer);
        
        // hardcoded threadgroup :(
        compute.Dispatch(0, THREAD_SIZE / 32, 1, 1);
        
        // it's scannin time
        int scanSteps = Mathf.CeilToInt(Mathf.Log(THREAD_SIZE, 2));
        for (int i = 0; i < scanSteps; i++)
        {
            int stride = 1 << i;
            
            compute.SetInt(stridePropertyId, stride);
            
            // ping pong buffers while scanning
            if (i % 2 == 0)
            {
                compute.SetBuffer(1, deltasPropertyId, deltasBuffer);
                compute.SetBuffer(1, deltasBPropertyId, deltasBufferB);
            }
            else
            {
                compute.SetBuffer(1, deltasPropertyId, deltasBufferB);
                compute.SetBuffer(1, deltasBPropertyId, deltasBuffer);
            }
            
            compute.Dispatch(1, THREAD_SIZE / 32, 1, 1);
        }
        
        // finally, get the final result via InterlockedAdd
        var finalDeltaBuffer = (scanSteps % 2 == 0) ?  deltasBuffer : deltasBufferB;
        compute.SetBuffer(2, deltasPropertyId, finalDeltaBuffer);
        compute.SetBuffer(2, resultPropertyId, resultBuffer);
        compute.Dispatch(2, THREAD_SIZE / 32, 1, 1);
    }

    public override void Cleanup()
    {
        // Debug.Log(length);
        resultBuffer.Release();
        deltasBuffer.Release();
        deltasBufferB.Release();
    }

    public override void GetData()
    {
        var bytes = new int[1];
        resultBuffer.GetData(bytes);
        /*Debug.Log(bytes[0]);
        
        var bytes2 = new int[100];
        deltasBufferB.GetData(bytes);
        Debug.Log(string.Join("|", bytes2));*/
    }
}