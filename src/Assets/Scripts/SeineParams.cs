using UnityEngine;

/// <summary>
/// Параметры для построения сетки
/// </summary>
[System.Serializable]
public class SeineParams
{
    /// <summary>
    /// Координаты точек на прямых LU-LD, RU-RD, LD-RD, LU-RU
    /// </summary>
    public Vector2[,] seine;

    /// <summary>
    /// Хранятся кожффициенты прямых k и b между точек
    /// </summary>
    public Vector2[,] koef;

    /// <summary>
    /// Размерность
    /// </summary>
    public int resolution = 3;

    /// <summary>
    /// Отрисовывать сетку или нет
    /// </summary>
    public bool DrawSeineGUI;
}