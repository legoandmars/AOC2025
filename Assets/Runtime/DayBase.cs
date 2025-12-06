using System.Diagnostics;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

public abstract class DayBase : MonoBehaviour
{
    [SerializeField]
    protected TextAsset inputFile;

    [SerializeField]
    protected TMP_Text text;

    protected string input;
    protected NativeArray<byte> inputBytes;
    protected GraphicsBuffer inputBuffer;
    protected int inputLength;

    private void Start()
    {
        #if UNITY_EDITOR
            Run(1);
        #else
            Run(1000000);
        #endif
    }
    
    public void Run(int count)
    {
        input = inputFile.text;
        inputBytes = inputFile.GetData<byte>();
        inputLength = inputBytes.Length;
        inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, inputLength / 4, 4);
        inputBuffer.SetData(inputBytes);
        
        Setup();

        // run a few times just to make sure that PSOs compiled and whatnot
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < count; i++)
        {
            RunCode();
        }

        GetData();
        
        // get data to ensure all is actually executing
        stopwatch.Stop();

        if (count == 10000)
        {
            UnityEngine.Debug.Log("Time elapsed (us): "+ (stopwatch.Elapsed.TotalMilliseconds / 10));
            text.text = "Time elapsed (us): " + (stopwatch.Elapsed.TotalMilliseconds / 10);
        }
        else if (count == 1000000)
        {
            UnityEngine.Debug.Log("Time elapsed (us): "+ (stopwatch.Elapsed.TotalMilliseconds / 1000));
            text.text = "Time elapsed (us): " + (stopwatch.Elapsed.TotalMilliseconds / 1000);
        }
        else if (count == 1)
        {
            UnityEngine.Debug.Log("Time elapsed (us): "+ (stopwatch.Elapsed.TotalMilliseconds * 1000));
            text.text = "Time elapsed (us): " + (stopwatch.Elapsed.TotalMilliseconds * 1000);
        }

        inputBytes.Dispose();
        inputBuffer.Release();
        Cleanup();
    }

    private void RunInternal()
    {
        RunCode();
    }
    
    public abstract void Setup();

    public abstract void RunCode();

    public abstract void Cleanup();
    
    public abstract void GetData();
    
    /*
    public static unsafe (NativeArray<byte>, int) RoundToNearestBytes(NativeArray<byte> arr, int length)
    {
        if (arr.Length % 4 == 0)
            return (arr, length);

        var roundedUp = Mathf.CeilToInt(arr.Length / 4f) * 4;
        void* ptr = NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(arr);
        var fake = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, roundedUp, Allocator.None);
        return (fake, roundedUp);
    }*/
}
