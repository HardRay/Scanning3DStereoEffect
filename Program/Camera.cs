using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using Num = System.Numerics;

namespace Program
{
    // Абстрактный класс камеры, который обрабатывает входные данные и вычисляет соответствующие Эйлеровы углы, векторы и матрицы для использования в OpenGL
    class Camera
    {
        // Определяет несколько возможных вариантов движения камеры. Используется в качестве абстракции, чтобы держаться подальше от специфичных для оконной системы методов ввода
        public enum Camera_Movement
        {
            FORWARD,
            BACKWARD,
            LEFT,
            RIGHT
        };

        // Параметры камеры по умолчанию
        const float YAW = -135.0f;
        const float PITCH = -35.0f;
        const float SPEED = 2.5f;
        const float SENSITIVITY = 0.1f;
        const float ZOOM = 45.0f;

        // Атрибуты камеры
        private Vector3 Position;
        private Vector3 Front;
        private Vector3 Up;
        private Vector3 Right;
        private Vector3 WorldUp;
        // углы Эйлера
        private float Yaw;
        private float Pitch;
        // Настройки камеры
        private float MovementSpeed;
        private float MouseSensitivity;
        public float Zoom;

        // Конструктор, использующий векторы
        public Camera(Vector3 position, Vector3 up, float yaw = YAW, float pitch = PITCH)
        {
            Front = new Vector3(0.0f, 0.0f, 0.0f);
            MovementSpeed = SPEED;
            MouseSensitivity = SENSITIVITY;
            Zoom = ZOOM;
            Position = position;
            WorldUp = up;
            Yaw = yaw;
            Pitch = pitch;
            updateCameraVectors();
        }

        //Перегрузки конструктора для значений по умолчанию
        public Camera() : this(new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f)) { }
        public Camera(Vector3 position) : this(position, new Vector3(0.0f, 0.0f, 1.0f)) { }

        // Конструктор, использующие скаляры
        public Camera(float posX, float posY, float posZ, float upX, float upY, float upZ, float yaw, float pitch)
        {
            Front = new Vector3(0.0f, 0.0f, 0.0f);
            MovementSpeed = SPEED;
            MouseSensitivity = SENSITIVITY;
            Zoom = ZOOM;
            Position = new Vector3(posX, posY, posZ);
            WorldUp = new Vector3(upX, upY, upZ);
            Yaw = yaw;
            Pitch = pitch;
            updateCameraVectors();
        }

        // Возвращает матрицу вида, вычисленную с использованием углов Эйлера и LookAt-матрицы 
        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        //Обрабатываем входные данные, полученные от любой клавиатуроподобной системы ввода. Принимаем входной параметр в виде определенного камерой перечисления (для абстрагирования его от оконных систем)
        public void ProcessKeyboard(Camera_Movement direction)
        {
            float velocity = MovementSpeed;
            if (direction == Camera_Movement.FORWARD)
                Position += Front * velocity;
            if (direction == Camera_Movement.BACKWARD)
                Position -= Front * velocity;
            if (direction == Camera_Movement.LEFT)
                Position -= Right * velocity;
            if (direction == Camera_Movement.RIGHT)
                Position += Right * velocity;
        }

        //Обрабатываем входные данные, полученные от системы ввода с помощью мыши. Ожидаем в качестве параметров значения смещения как в направлении X, так и в направлении Y.
        public void ProcessMouseMovement(float xoffset, float yoffset, bool constrainPitch = true)
        {
            xoffset *= MouseSensitivity;
            yoffset *= MouseSensitivity;

            Yaw += xoffset;
            Pitch += yoffset;

            // Убеждаемся, что когда тангаж выходит за пределы обзора, экран не переворачивается
            if (constrainPitch)
            {
                if (Pitch > 89.0f)
                    Pitch = 89.0f;
                if (Pitch < -89.0f)
                    Pitch = -89.0f;
            }

            // Обновляем значения вектора-прямо, вектора-вправо и вектора-вверх, используя обновленные значения углов Эйлера
            updateCameraVectors();
        }

        // Обрабатывает входные данные, полученные от события колеса прокрутки мыши. Интересуют только входные данные на вертикальную ось колесика 
        public void ProcessMouseScroll(float yoffset)
        {
            if (Zoom >= 1.0f && Zoom <= 45.0f)
                Zoom -= yoffset;
            if (Zoom <= 1.0f)
                Zoom = 1.0f;
            if (Zoom >= 45.0f)
                Zoom = 45.0f;
        }

        // Вычисляет вектор-прямо по (обновленным) углам Эйлера камеры
        private void updateCameraVectors()
        {
            // Вычисляем новый вектор-прямо
            Vector3 front;
            front.Y = (float)(Math.Cos(Yaw * Math.PI / 180) * Math.Cos(Pitch * Math.PI / 180));
            front.Z = (float)Math.Sin(Pitch * Math.PI / 180);
            front.X = (float)(Math.Sin(Yaw * Math.PI / 180) * Math.Cos(Pitch * Math.PI / 180));
            Front = front.Normalized();
            // Также пересчитываем вектор-вправо и вектор-вверх
            Right = NumCross(Front, WorldUp).Normalized();  // Нормализуем векторы, потому что их длина становится стремится к 0 тем больше, чем больше вы смотрите вверх или вниз, что приводит к более медленному движению.
            Up = NumCross(Right, Front).Normalized();
        }

        private Vector3 NumCross(Vector3 first, Vector3 second)
        {
            Num.Vector3 vec1 = new Num.Vector3(first.X, first.Y, first.Z);
            Num.Vector3 vec2 = new Num.Vector3(second.X, second.Y, second.Z);
            Num.Vector3 result = Num.Vector3.Cross(vec1, vec2);
            return new Vector3(result.X, result.Y, result.Z);
        }
    }
}
