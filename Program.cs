using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Renderer
{
	class Program
	{
		public static void Main()
		{
			const int width = 1000;
			const int height = 800;

			SetTraceLogLevel(TraceLogLevel.LOG_NONE);
			InitWindow(width, height, "Renderer");

			ImguiController controller = new ImguiController();

			Screen screen = new Screen();

			controller.Load(width, height);
			screen.Load();

			while (!WindowShouldClose())
			{
				float dt = GetFrameTime();

				controller.Update(dt);
				screen.Update(dt);

				BeginDrawing();

				ClearBackground(Color.BLACK);

				controller.Draw();
				screen.Draw();

				EndDrawing();
			}

			controller.Dispose();

			CloseWindow();
		}
	}
}