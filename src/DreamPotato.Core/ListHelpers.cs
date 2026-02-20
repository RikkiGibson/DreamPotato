
namespace DreamPotato.Core;

public static class ListHelpers
{
    public static void Push<T>(this List<T> list, T item)
        => list.Add(item);

    public static T Pop<T>(this List<T> list)
    {
        var index = list.Count - 1;
        var item = list[index];
        list.RemoveAt(index);
        return item;
    }
}