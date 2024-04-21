namespace CalmOnion.GAA;


/// <summary>
/// Basic dictionary that enables querying by value.
/// </summary>
public class BiDictionary<TKey, TValue>
	where TKey : notnull
	where TValue : notnull
{
	protected readonly Dictionary<TKey, TValue> normalDict = [];
	protected readonly Dictionary<TValue, TKey> reverseDict = [];

	public TValue this[TKey key]
	{
		get
		{
			return normalDict[key];
		}
		set
		{
			normalDict[key] = value;
			reverseDict[value] = key;
		}
	}

	public TValue GetByKey(TKey key) => normalDict[key];
	public TKey GetByValue(TValue value) => reverseDict[value];
	public Dictionary<TKey, TValue>.KeyCollection Keys => normalDict.Keys;
	public Dictionary<TValue, TKey>.KeyCollection Values => reverseDict.Keys;
}
