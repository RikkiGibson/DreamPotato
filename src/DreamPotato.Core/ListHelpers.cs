
namespace DreamPotato.Core;

public static class ListHelpers
{
    public static void Push<T>(this List<T> list, T item)
        => list.Add(item);

    public static T? Pop<T>(this List<T> list)
    {
        var count = list.Count;
        if (count == 0)
            return default;

        var index = count - 1;
        var item = list[index];
        list.RemoveAt(index);
        return item;
    }
}