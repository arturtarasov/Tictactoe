using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//скрипт висит на поле из кубиков с триггером,
//при нажатии считываем позицию кубика (x, z) и передаем в массив board[,]
//и на этом месте инициализируем префаб oPrefab
public class ClickCube : MonoBehaviour
{
    private Vector3 vector = new Vector3(0, 0, 0);
    private static float x;
    private static float z;
    void Awake()
    {
        x = 0f;
        z = 0f;
    }
    void Start()
    {

    }
    
    void Update()
    {

    }
    void OnMouseDown() //кнопка мыши нажата
    {
        vector = transform.position;
        x = vector.x;
        z = vector.z;
    }
    public int getX()
    {
        return (int)x;
    }
    public int getZ()
    {
        return (int)z;
    }
}