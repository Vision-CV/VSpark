using System.Reflection;

using VSpark.Models.Data;

namespace VSpark.Models.Extensions;

public static class IncidentDataExtensions
{
    public static void MergeWith(this IncidentData basedOn, IncidentData newVersion)
    {
        foreach (PropertyInfo property in newVersion.GetType().GetProperties())
        {
            object newValue = property.GetValue(newVersion)!;

            if (!property.GetValue(basedOn)!.Equals(newValue))
                property.SetValue(basedOn, newValue);
        }
    }
}
