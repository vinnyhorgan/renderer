using Raylib_cs;
using ImGuiNET;
using ObjLoader.Loader.Loaders;

using System;
using System.Numerics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

using static Raylib_cs.Raylib;

namespace Renderer
{
	struct Col
	{
		public int r, g, b;

		public Col(int red, int green, int blue)
		{
			r = red;
			g = green;
			b = blue;
		}
	}

	struct Triangle
	{
		public Vector3[] p;
		public Col color;

		public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
		{
			p = new Vector3[3];

			p[0] = v0;
			p[1] = v1;
			p[2] = v2;

			color = new Col(255, 255, 255);
		}
	}

	struct Mesh
	{
		public List<Triangle> tris;

		public Mesh(params Triangle[] triangles)
		{
			tris = new List<Triangle>();

			for (int i = 0; i < triangles.Length; i++)
			{
				tris.Add(triangles[i]);
			}
		}
	}

	struct Mat4x4{
		public float[,] m;

		public Mat4x4()
		{
			m = new float[4,4];
			Array.Clear(m, 0, m.Length);
		}
	}

	class Screen
	{
		Mesh modelMesh;
		Mat4x4 projMatrix;

		Mat4x4 rotXMatrix;
		Mat4x4 rotZMatrix;

		Vector3 camera;

		float theta;

		LoadResult model;

		private void MultiplyMatrixVector(ref Vector3 i, ref Vector3 o, ref Mat4x4 m)
		{
			o.X = i.X * m.m[0, 0] + i.Y * m.m[1, 0] + i.Z * m.m[2, 0] + m.m[3, 0];
			o.Y = i.X * m.m[0, 1] + i.Y * m.m[1, 1] + i.Z * m.m[2, 1] + m.m[3, 1];
			o.Z = i.X * m.m[0, 2] + i.Y * m.m[1, 2] + i.Z * m.m[2, 2] + m.m[3, 2];

			float w = i.X * m.m[0, 3] + i.Y * m.m[1, 3] + i.Z * m.m[2, 3] + m.m[3, 3];

			if (w != 0.0f)
			{
				o.X /= w;
				o.Y /= w;
				o.Z /= w;
			}
		}

		public void Load()
		{
			modelMesh = new Mesh(new Triangle(new Vector3(), new Vector3(), new Vector3()));

			float near = 0.1f;
			float far = 1000.0f;
			float fov = 90.0f;
			float aspectRatio = (float)GetScreenHeight() / (float)GetScreenWidth();
			float fovRad = 1.0f / MathF.Tan(fov * 0.5f / 180.0f * 3.14159f);

			projMatrix = new Mat4x4();

			projMatrix.m[0, 0] = aspectRatio * fovRad;
			projMatrix.m[1, 1] = fovRad;
			projMatrix.m[2, 2] = far / (far - near);
			projMatrix.m[3, 2] = (-far * near) / (far - near);
			projMatrix.m[2, 3] = 1.0f;
			projMatrix.m[3, 3] = 0.0f;

			rotXMatrix = new Mat4x4();
			rotZMatrix = new Mat4x4();

			theta = 0.0f;

			camera = new Vector3(0.0f, 0.0f, 0.0f);

			ObjLoaderFactory objLoaderFactory = new ObjLoaderFactory();
			IObjLoader objLoader = objLoaderFactory.Create();

			FileStream obj = new FileStream("assets/charizard.obj", FileMode.Open);
			model = objLoader.Load(obj);

			foreach (var face in model.Groups[0].Faces)
			{
				modelMesh.tris.Add(new Triangle(
					new Vector3(model.Vertices[face[0].VertexIndex - 1].X, model.Vertices[face[0].VertexIndex - 1].Y, model.Vertices[face[0].VertexIndex - 1].Z),
					new Vector3(model.Vertices[face[1].VertexIndex - 1].X, model.Vertices[face[1].VertexIndex - 1].Y, model.Vertices[face[1].VertexIndex - 1].Z),
					new Vector3(model.Vertices[face[2].VertexIndex - 1].X, model.Vertices[face[2].VertexIndex - 1].Y, model.Vertices[face[2].VertexIndex - 1].Z)
				));
			}
		}

		public void Update(float dt)
		{
			// ImGui.ShowDemoWindow();

			theta += 1.0f * dt;

			rotZMatrix.m[0, 0] = MathF.Cos(theta);
			rotZMatrix.m[0, 1] = MathF.Sin(theta);
			rotZMatrix.m[1, 0] = -MathF.Sin(theta);
			rotZMatrix.m[1, 1] = MathF.Cos(theta);
			rotZMatrix.m[2, 2] = 1;
			rotZMatrix.m[3, 3] = 1;

			rotXMatrix.m[0, 0] = 1;
			rotXMatrix.m[1, 1] = MathF.Cos(theta * 0.5f);
			rotXMatrix.m[1, 2] = MathF.Sin(theta * 0.5f);
			rotXMatrix.m[2, 1] = -MathF.Sin(theta * 0.5f);
			rotXMatrix.m[2, 2] = MathF.Cos(theta * 0.5f);
			rotXMatrix.m[3, 3] = 1;
		}

		public void Draw()
		{
			DrawText("FPS: " + GetFPS(), 10, 10, 50, Color.WHITE);

			List<Triangle> toDraw = new List<Triangle>();

			foreach (Triangle tri in modelMesh.tris)
			{
				Triangle projected = new Triangle(new Vector3(), new Vector3(), new Vector3());
				Triangle translated = new Triangle(new Vector3(), new Vector3(), new Vector3());
				Triangle rotatedZ = new Triangle(new Vector3(), new Vector3(), new Vector3());
				Triangle rotatedZX = new Triangle(new Vector3(), new Vector3(), new Vector3());

				// Rotate

				MultiplyMatrixVector(ref tri.p[0], ref rotatedZ.p[0], ref rotZMatrix);
				MultiplyMatrixVector(ref tri.p[1], ref rotatedZ.p[1], ref rotZMatrix);
				MultiplyMatrixVector(ref tri.p[2], ref rotatedZ.p[2], ref rotZMatrix);

				MultiplyMatrixVector(ref rotatedZ.p[0], ref rotatedZX.p[0], ref rotXMatrix);
				MultiplyMatrixVector(ref rotatedZ.p[1], ref rotatedZX.p[1], ref rotXMatrix);
				MultiplyMatrixVector(ref rotatedZ.p[2], ref rotatedZX.p[2], ref rotXMatrix);

				// Translate

				translated.p[0] = rotatedZX.p[0];
				translated.p[1] = rotatedZX.p[1];
				translated.p[2] = rotatedZX.p[2];

				translated.p[0].Z = rotatedZX.p[0].Z + 8.0f;
				translated.p[1].Z = rotatedZX.p[1].Z + 8.0f;
				translated.p[2].Z = rotatedZX.p[2].Z + 8.0f;

				// New shit

				Vector3 normal, line1, line2;

				line1.X = translated.p[1].X - translated.p[0].X;
				line1.Y = translated.p[1].Y - translated.p[0].Y;
				line1.Z = translated.p[1].Z - translated.p[0].Z;

				line2.X = translated.p[2].X - translated.p[0].X;
				line2.Y = translated.p[2].Y - translated.p[0].Y;
				line2.Z = translated.p[2].Z - translated.p[0].Z;

				normal.X = line1.Y * line2.Z - line1.Z * line2.Y;
				normal.Y = line1.Z * line2.X - line1.X * line2.Z;
				normal.Z = line1.X * line2.Y - line1.Y * line2.X;

				normal = Vector3.Normalize(normal);

				if(normal.X * (translated.p[0].X - camera.X) +
					normal.Y * (translated.p[0].Y - camera.Y) +
					normal.Z * (translated.p[0].Z - camera.Z) < 0.0f)
				{
					Vector3 lightDirection = new Vector3(0.0f, 0.0f, -1.0f);

					lightDirection = Vector3.Normalize(lightDirection);

					float dp = Vector3.Dot(lightDirection, normal);

					projected.color.r = (int)(dp * 255);
					projected.color.g = (int)(dp * 255);
					projected.color.b = (int)(dp * 255);

					if (projected.color.r < 0) {projected.color.r = 0;}
					if (projected.color.g < 0) {projected.color.g = 0;}
					if (projected.color.b < 0) {projected.color.b = 0;}

					// Project

					MultiplyMatrixVector(ref translated.p[0], ref projected.p[0], ref projMatrix);
					MultiplyMatrixVector(ref translated.p[1], ref projected.p[1], ref projMatrix);
					MultiplyMatrixVector(ref translated.p[2], ref projected.p[2], ref projMatrix);

					// Scale

					projected.p[0].X += 1.0f; projected.p[0].Y += 1.0f;
					projected.p[1].X += 1.0f; projected.p[1].Y += 1.0f;
					projected.p[2].X += 1.0f; projected.p[2].Y += 1.0f;

					projected.p[0].X *= 0.5f * (float)GetScreenWidth();
					projected.p[0].Y *= 0.5f * (float)GetScreenHeight();
					projected.p[1].X *= 0.5f * (float)GetScreenWidth();
					projected.p[1].Y *= 0.5f * (float)GetScreenHeight();
					projected.p[2].X *= 0.5f * (float)GetScreenWidth();
					projected.p[2].Y *= 0.5f * (float)GetScreenHeight();

					toDraw.Add(projected);
				}

				// Sort the fucking triangles

				toDraw.Sort((t1, t2) => {
					float z1 = (t1.p[0].Z + t1.p[1].Z + t1.p[2].Z) / 3.0f;
					float z2 = (t2.p[0].Z + t2.p[1].Z + t2.p[2].Z) / 3.0f;

					return z1.CompareTo(z2);
				});

				toDraw.Reverse();

				foreach (Triangle t in toDraw)
				{
					DrawTriangle(
						new Vector2(t.p[0].X, t.p[0].Y),
						new Vector2(t.p[1].X, t.p[1].Y),
						new Vector2(t.p[2].X, t.p[2].Y),
						new Color(t.color.r, t.color.g, t.color.b, 255)
					);
				}
			}
		}
	}
}