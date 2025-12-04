using System.Diagnostics;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

public abstract class DayBase : MonoBehaviour
{
    private const long RUN_COUNT = 1000000;


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
        Run();
    }
    
    public void Run()
    {
        input = inputFile.text;
        inputBytes = inputFile.GetData<byte>();
        inputLength = inputBytes.Length;
        Debug.Log(inputLength);
        Debug.Log(inputBytes.Length);
        inputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, inputLength / 4, 4);
        inputBuffer.SetData(inputBytes);
        
        Setup();

        // run a few times just to make sure that PSOs compiled and whatnot
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        for (int i = 0; i < RUN_COUNT; i++)
        {
            RunCode();
        }

        GetData();
        
        // get data to ensure all is actually executing
        stopwatch.Stop();

        if (RUN_COUNT == 1000)
        {
            UnityEngine.Debug.Log("Time elapsed (us): "+ (stopwatch.Elapsed.TotalMilliseconds));
            text.text = "Time elapsed (us): " + (stopwatch.Elapsed.TotalMilliseconds);
        }
        else
        {
            UnityEngine.Debug.Log("Time elapsed (us): "+ (stopwatch.Elapsed.TotalMilliseconds / 1000));
            text.text = "Time elapsed (us): " + (stopwatch.Elapsed.TotalMilliseconds / 1000);
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
