using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DebugCategories
{
    Default,
    ExecutionOrder,
    AnimationRigging,
}

public enum LogSeverity
{
    Info,
    Warning,
    Error,
    Debug
}

[DefaultExecutionOrder(-2)]
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Settings")]
    public bool DebugMode;
    [field: SerializeField] public bool DisplayCrossProductVisual { get; private set; }
    [field: SerializeField] public bool DisplayEdgeDetectionVisual { get; private set; }

    [SerializeField] List<DebugCategories> enabledCategories;

    private static HashSet<DebugCategories> activeCategories = new HashSet<DebugCategories>();

    #region Startup
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        foreach (var category in enabledCategories)
            EnableCategory(category);

        Log(LogSeverity.Debug, DebugCategories.ExecutionOrder, $"{this.GetType().Name} initialized");
    }

    private void Update()
    {
        foreach (var category in enabledCategories)
            EnableCategory(category);

    }
    #endregion

    #region Log Methods
    // Enable a category
    public static void EnableCategory(DebugCategories category)
    {
        if (!activeCategories.Contains(category))
        {
            activeCategories.Add(category);
        }
    }

    // Disable a category
    public static void DisableCategory(DebugCategories category)
    {
        if (activeCategories.Contains(category))
        {
            activeCategories.Remove(category);
        }
    }

    // Check if a category is enabled
    public static bool IsCategoryEnabled(DebugCategories category)
    {
        return activeCategories.Contains(category);
    }

    // Toggle a category on/off
    public static void ToggleCategory(DebugCategories category)
    {
        if (activeCategories.Contains(category))
        {
            activeCategories.Remove(category);
        }
        else
        {
            activeCategories.Add(category);
        }
    }

    // Log if category is enabled
    public void Log(LogSeverity level, DebugCategories category, string message, string tag = null)
    {
        string TimeStamp = System.DateTime.Now.ToString("HH:mm:ss.fff");

        if (DebugMode)
        {
            if (IsCategoryEnabled(category))
            {
                switch (level)
                {
                    case LogSeverity.Info:
                        Debug.Log($"<color=cyan>[{TimeStamp}] [INFO]</color> [{category}] [{tag}] \n {message} \n{System.Environment.StackTrace}");
                        break;
                    case LogSeverity.Warning:
                        Debug.LogWarning($"<color=orange>[{TimeStamp}] [WARNING]</color> [{category}] [{tag}] \n {message}  \n{System.Environment.StackTrace}");
                        break;
                    case LogSeverity.Error:
                        Debug.LogError($"<color=red>[{TimeStamp}] [ERROR]</color> [{category}] [{tag}] \n {message}  \n{System.Environment.StackTrace}");
                        break;
                    case LogSeverity.Debug:
                        Debug.Log($"<color=magenta>[{TimeStamp}] [DEBUG]</color> [{category}] [{tag}] \n {message}  \n{System.Environment.StackTrace}");
                        break;
                    default:
                        break;
                }

            }
        }
    }
    #endregion

    public void GetCrossProductData(float sign, Vector3 camForwardProjectedXZ, Vector3 crossProduct, Vector3 playerForward, Vector3 playerUp)
    {
        if (DisplayCrossProductVisual)
            CrossProductVisual.Instance.DisplayCrossProductVisual(sign, camForwardProjectedXZ, crossProduct, playerForward, playerUp);
    }

}
