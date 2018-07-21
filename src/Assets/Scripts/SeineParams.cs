using UnityEngine;

/// <summary>
/// ��������� ��� ���������� �����
/// </summary>
[System.Serializable]
public class SeineParams
{
    /// <summary>
    /// ���������� ����� �� ������ LU-LD, RU-RD, LD-RD, LU-RU
    /// </summary>
    public Vector2[,] seine;

    /// <summary>
    /// �������� ������������ ������ k � b ����� �����
    /// </summary>
    public Vector2[,] koef;

    /// <summary>
    /// �����������
    /// </summary>
    public int resolution = 3;

    /// <summary>
    /// ������������ ����� ��� ���
    /// </summary>
    public bool DrawSeineGUI;
}