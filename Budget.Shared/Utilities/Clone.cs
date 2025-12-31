namespace Budget.Shared.Utilities;

public static class Clone
{



  /// <summary>
  /// Creates a deep clone of the given object using Mapster.
  /// </summary>
  public static T DeepClone<T>(T source)
  {
    if(source == null)
      throw new ArgumentNullException(nameof(source), "Source object cannot be null.");

    var deepConfig = new TypeAdapterConfig();
    deepConfig.Default.PreserveReference(false); 
    deepConfig.Default.ShallowCopyForSameType(false); 
    return source.Adapt<T>(deepConfig);
  }

  /// <summary>
  /// Creates a shallow clone of the given object using Mapster.
  /// </summary>
  public static T ShallowClone<T>(T source)
  {
    if(source == null)
      throw new ArgumentNullException(nameof(source), "Source object cannot be null.");

    // Local config for shallow copy
    var shallowConfig = new TypeAdapterConfig();
    shallowConfig.Default.PreserveReference(true); // Keep same references
    shallowConfig.Default.ShallowCopyForSameType(true); // Copy only top-level properties

    return source.Adapt<T>(shallowConfig);
  }
}