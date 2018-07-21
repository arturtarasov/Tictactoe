using UnityEngine;

[System.Serializable]
public class ScreenToWorld
{
	public ScreenToWorld(Rect rectangle)
	{
		Rectangle = rectangle;
	}

	private Rect rectangle;

	public Rect Rectangle
	{
		get { return rectangle; }
		set
		{
			if (rectangle != value)
			{
				rectangle = value;
				World = new Vector2(value.x, value.y);
			}
		}
	}

	public Vector2 World;
}