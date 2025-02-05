﻿using Editor.Editor;
using Editor.Engine.Interfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Editor.Engine
{
    internal class Terrain : ISelectable, IRenderable
    {
        public VertexPositionNormalTexture[] Vertices { get; set; } // Vertex array
        public VertexBuffer VertexBuffer { get; set; } // Vertex Buffer
        public int[] Indices { get; set; } // Index array
        public IndexBuffer IndexBuffer { get; set; } // Index buffer
        public float[,] Heights { get; set; } // Array of vertex heights
        public int Width { get; set; } // Number of vertices on x axis
        public int Length { get; set; } // Number of vertices on z axis
        public int Height { get; set; } // Terrain height factor
        public int VertexCount { get; set; } // Number of vertices
        public int IndexCount { get; set; } // Number of indices
        public GraphicsDevice Device { get; set; } // The graphics device for rendering
        public Vector3 LightDirection { get; set; } // Direction light is emanating from
        public Texture2D HeightMap { get; set; } // Heightmap texture
        public Texture2D BaseTexture { get; set; } // The terrain diffuse texture
        public bool Selected { get; set; } = false;
        public Material Material { get; private set; }

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Rotation { get; set; } = Vector3.Zero;
        public float Scale { get; set; } = 1.0f;

        public Terrain(Effect _effect, Texture2D _heightMap, Texture2D _baseTexture, int _height, GraphicsDevice _device)
        {
            Material = new Material();
            HeightMap = _heightMap;
            Material.Diffuse = _baseTexture;
            Material.Effect = _effect;
            BaseTexture = _baseTexture;
            Device = _device;
            Width = _heightMap.Width;
            Length = _heightMap.Height;
            Height = _height;
            LightDirection = new Vector3(0, 1, 1);
            // 1 vertex per pixel
            VertexCount = Width * Length;
            // (Width - 1) * (Length - 1) cells, 2 triangles per cell, 3 indices per triangle
            IndexCount = (Width - 1) * (Length - 1) * 6;

            GetHeights();
            CreateVertices();
            CreateIndices();
            GenNormals();

            VertexBuffer.SetData<VertexPositionNormalTexture>(Vertices);
            IndexBuffer.SetData<int>(Indices);
        }

        private void GetHeights()
        {
            // Extract pixel data
            Color[] heightMapData = new Color[HeightMap.Width * HeightMap.Height];
            HeightMap.GetData<Color>(heightMapData);
            // Create heights[,] array
            Heights = new float[Width, Length];
            // For each pixel
            for (int y = 0; y < Length; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // Get color value (0 - 255)
                    float amt = heightMapData[y * Width + x].R;
                    // Scale to (0 - 1)
                    amt /= 255.0f;
                    // Multiply by max height to get final height
                    Heights[x, y] = amt * Height;
                }
            }
        }

        private void CreateVertices()
        {
            VertexBuffer = new VertexBuffer(Device, typeof(VertexPositionNormalTexture), VertexCount, BufferUsage.WriteOnly);

            Vertices = new VertexPositionNormalTexture[VertexCount];
            for(int y = 0; y < Length; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    int index = y * Width + x;
                    Vertices[index] = new VertexPositionNormalTexture();
                    Vertices[index].Position = new Vector3(x, Heights[x, y], y);
                    Vertices[index].Normal = new Vector3(0, 0, 0);
                    Vertices[index].TextureCoordinate = new Vector2((float)x / Width, (float)y / Length);
                }
            }
        }

        private void CreateIndices()
        {
            IndexBuffer = new IndexBuffer(Device, IndexElementSize.ThirtyTwoBits, IndexCount, BufferUsage.WriteOnly);

            Indices = new int[IndexCount];
            int i = 0;
            // For each cell
            for(int y = 0; y < Length - 1; y++) 
            { 
                for(int x = 0; x < Width - 1; x++)
                {
                    // Find the indices of the corners
                    int upperLeft = y * Width + x;
                    int upperRight = upperLeft + 1;
                    int lowerLeft = upperLeft + Width;
                    int lowerRight = lowerLeft + 1;
                    // Specify upper triangle
                    Indices[i++] = upperLeft;
                    Indices[i++] = upperRight;
                    Indices[i++] = lowerLeft;
                    // Specify lower triangle
                    Indices[i++] = lowerLeft;
                    Indices[i++] = upperRight;
                    Indices[i++] = lowerRight;
                }
            }
        }

        private void GenNormals()
        {
            // For each triangle
            for (int i = 0; i < IndexCount; i += 3)
            {
                // Find the position of each cordner of the triangle
                Vector3 v1 = Vertices[Indices[i]].Position;
                Vector3 v2 = Vertices[Indices[i + 1]].Position;
                Vector3 v3 = Vertices[Indices[i + 2]].Position;
                // Cross the vectors between the cordenrs to get the normal
                Vector3 normal = Vector3.Cross(v1 - v3, v1 - v2);
                normal.Normalize();
                // Add the influence of the normal to each vertex in the triangle
                Vertices[Indices[i]].Normal += normal;
                Vertices[Indices[i + 1]].Normal += normal;
                Vertices[Indices[i + 2]].Normal += normal;
            }

            // Average the influences of the triagnles touching eac vertex
            for (int i = 0; i < VertexCount; i++)
            {
                Vertices[i].Normal.Normalize();
            }
        }

        public void SetTexture(GameEditor _game, string _texture)
        {
            if(_texture == "DefaultTexture")
            {
                Material.Diffuse = _game.DefaultTexture;
            } else
            {
                Material.Diffuse = _game.Content.Load<Texture>(_texture);
            }
            Material.Diffuse.Tag = _texture;
        }

        public void SetShader(GameEditor _game, string _shader)
        {
            if (_shader == "DefaultEffect")
            {
                Material.Effect = _game.DefaultEffect;
            }
            else
            {
                Material.Effect = _game.Content.Load<Effect>(_shader);
            }
            Material.Effect.Tag = _shader;
        }

        public Matrix GetTransform()
        {
            return Matrix.CreateScale(Scale) * Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) * Matrix.CreateTranslation(Position);
        }

        public void Render()
        {
            Device.SetVertexBuffer(VertexBuffer);
            Device.Indices = IndexBuffer;

            foreach(EffectPass pass in Material.Effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, IndexCount / 3);  
            }
        }

    }
}
