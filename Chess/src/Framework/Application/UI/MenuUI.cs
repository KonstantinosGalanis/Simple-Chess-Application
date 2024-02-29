using Raylib_cs;
using System.Numerics;
using System;
using System.IO;

namespace ChessChallenge.Application
{
    public static class MenuUI
    {
        private static int selectedDifficulty = 3;
        private static bool isDragging = false;
        // Add a variable to store the difficulty level
        private static ChallengeController.PlayerColor selectedPlayerColor = ChallengeController.PlayerColor.White;
       
        public static void DrawButtons(ChallengeController controller)
        {
            Vector2 buttonPos = UIHelper.Scale(new Vector2(260, 210));
            Vector2 buttonSize = UIHelper.Scale(new Vector2(260, 55));
            float spacing = buttonSize.Y * 1.2f;
            float breakSpacing = spacing * 0.6f;

            // Game Buttons
            if (NextButtonInRow("Human vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = selectedPlayerColor == ChallengeController.PlayerColor.White ? ChallengeController.PlayerType.Human : ChallengeController.PlayerType.MyBot;
                var blackType = whiteType ==  ChallengeController.PlayerType.Human ? ChallengeController.PlayerType.MyBot : ChallengeController.PlayerType.Human;
                controller.StartNewGame(whiteType, blackType, selectedDifficulty);
            }

            buttonPos.Y += breakSpacing;
            if (NextButtonInRow("Player Color", ref buttonPos, spacing, buttonSize))
            {
                
            }
            buttonPos.Y -= breakSpacing;
            buttonPos.Y += 5;
            DrawPlayerColorButtons(ref buttonPos, spacing, buttonSize, ref selectedPlayerColor);

            buttonPos.Y += breakSpacing+10;
            if (NextButtonInRow("MyBot vs MyBot", ref buttonPos, spacing, buttonSize))
            {
                controller.StartNewBotMatch(ChallengeController.PlayerType.MyBot, ChallengeController.PlayerType.MyBot, selectedDifficulty);
            }

            if (NextButtonInRow("Human vs Human", ref buttonPos, spacing, buttonSize))
            {
                var whiteType = ChallengeController.PlayerType.Human;
                var blackType = ChallengeController.PlayerType.Human;
                controller.StartNewGame(whiteType, blackType, selectedDifficulty);
            }

            // Difficulty slider
            buttonPos.Y += breakSpacing;
            if (NextButtonInRow("Difficulty", ref buttonPos, spacing, buttonSize))
            {
                
            }
            buttonPos.Y -= breakSpacing;
            buttonPos.Y += 10;
            DrawSlider(ref buttonPos, spacing, buttonSize, ref selectedDifficulty);

            // Page buttons
            buttonPos.Y += breakSpacing+10;

            if (NextButtonInRow("Save Games", ref buttonPos, spacing, buttonSize))
            {
                string pgns = controller.AllPGNs;
                string directoryPath = Path.Combine(FileHelper.AppDataPath, "Games");
                Directory.CreateDirectory(directoryPath);
                string fileName = FileHelper.GetUniqueFileName(directoryPath, "games", ".txt");
                string fullPath = Path.Combine(directoryPath, fileName);
                File.WriteAllText(fullPath, pgns);
                ConsoleHelper.Log("Saved games to " + fullPath, false, ConsoleColor.Blue);
            }

            // Window and quit buttons
            buttonPos.Y += breakSpacing;

            bool isBigWindow = Raylib.GetScreenWidth() > Settings.ScreenSizeSmall.X;
            string windowButtonName = isBigWindow ? "Smaller Window" : "Bigger Window";
            if (NextButtonInRow(windowButtonName, ref buttonPos, spacing, buttonSize))
            {
                Program.SetWindowSize(isBigWindow ? Settings.ScreenSizeSmall : Settings.ScreenSizeBig);
            }
            if (NextButtonInRow("Exit (ESC)", ref buttonPos, spacing, buttonSize))
            {
                Environment.Exit(0);
            }

            bool NextButtonInRow(string name, ref Vector2 pos, float spacingY, Vector2 size)
            {
                bool pressed = UIHelper.Button(name, pos, size);
                pos.Y += spacingY;
                return pressed;
            }

            void DrawPlayerColorButtons(ref Vector2 pos, float spacingY, Vector2 size, ref ChallengeController.PlayerColor selectedPlayerColor)
            {

                float buttonWidth = (size.X / 2) -6;
                float buttonHeight = size.Y;

                Rectangle whiteButtonRect = new Rectangle(pos.X - 85, pos.Y, buttonWidth, buttonHeight);
                Rectangle blackButtonRect = new Rectangle(pos.X + 5 , pos.Y, buttonWidth, buttonHeight);

                // Check if mouse is over the White button
                bool isMouseOverWhiteButton = Raylib.GetMouseX() >= whiteButtonRect.x && Raylib.GetMouseX() <= whiteButtonRect.x + whiteButtonRect.width &&
                                            Raylib.GetMouseY() >= whiteButtonRect.y && Raylib.GetMouseY() <= whiteButtonRect.y + whiteButtonRect.height;

                // Check if mouse is over the Black button
                bool isMouseOverBlackButton = Raylib.GetMouseX() >= blackButtonRect.x && Raylib.GetMouseX() <= blackButtonRect.x + blackButtonRect.width &&
                                            Raylib.GetMouseY() >= blackButtonRect.y && Raylib.GetMouseY() <= blackButtonRect.y + blackButtonRect.height;

                // Draw White button
                Color whiteButtonColor = isMouseOverWhiteButton ? Color.LIGHTGRAY : Color.WHITE;
                if (selectedPlayerColor == ChallengeController.PlayerColor.White)
                {
                    whiteButtonColor = Color.DARKGRAY; // Make it darker when selected
                }
                Raylib.DrawRectangleRec(whiteButtonRect, whiteButtonColor);
                Raylib.DrawText("White", (int)(pos.X - 70), (int)(pos.Y + 10), 20, isMouseOverWhiteButton ? Color.DARKGRAY : Color.BLACK);

                // Draw Black button
                Color blackButtonColor = isMouseOverBlackButton ? Color.LIGHTGRAY : Color.WHITE;
                if (selectedPlayerColor == ChallengeController.PlayerColor.Black)
                {
                    blackButtonColor = Color.DARKGRAY; // Make it darker when selected
                }
                Raylib.DrawRectangleRec(blackButtonRect, blackButtonColor);
                Raylib.DrawText("Black", (int)(pos.X + 15), (int)(pos.Y + 10), 20, isMouseOverBlackButton ? Color.DARKGRAY : Color.BLACK);

                // Check for button press
                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON))
                {
                    if (isMouseOverWhiteButton)
                    {
                        selectedPlayerColor = ChallengeController.PlayerColor.White;
                    }
                    else if (isMouseOverBlackButton)
                    {
                        selectedPlayerColor = ChallengeController.PlayerColor.Black;
                    }
                }

                pos.Y += spacingY;
            }


            void DrawSlider( ref Vector2 pos, float spacingY, Vector2 size, ref int value)
            {
                // Adjust the starting position of the slider to align with other buttons
                float sliderWidth = size.X * 0.8f;
                float sliderPosX = pos.X - 70;
                float sliderPosY = pos.Y;
                float sliderHeight = size.Y * 0.2f;
                // Adjust the horizontal position of the "Difficulty Level" text
                Vector2 textPos = new Vector2(pos.X - 50, pos.Y - 30); // Adjust the values as needed

                // Draw the slider bar
                Raylib.DrawRectangle((int)sliderPosX, (int)sliderPosY, (int)sliderWidth, (int)sliderHeight, Color.LIGHTGRAY);

                // Calculate the width of each difficulty level section
                float sectionWidth = sliderWidth / 4;

                // Calculate the position of the circle based on the selected difficulty level
                float circleX = sliderPosX + Math.Clamp(value, 1, 7) / 2.0f * sectionWidth;

                // Check if the mouse is over the circle
                bool isMouseOverCircle = Raylib.GetMouseX() > circleX - 20 && Raylib.GetMouseX() < circleX + 20 &&
                                        Raylib.GetMouseY() > sliderPosY && Raylib.GetMouseY() < sliderPosY + sliderHeight;

                // Check for mouse press to start dragging
                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_LEFT_BUTTON) && isMouseOverCircle)
                {
                    isDragging = true;
                }

                // Check for mouse release to stop dragging
                if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_LEFT_BUTTON))
                {
                    isDragging = false;
                }

                // Update the selected difficulty level based on the slider position while dragging
                if (isDragging)
                {
                    float mouseX = Raylib.GetMouseX();
                    int selectedSection = (int)((mouseX - sliderPosX) / sectionWidth);
                    value = Math.Clamp(selectedSection * 2 + 1, 1, 7);
                }

                // Draw the circle representing the selected difficulty level
                Raylib.DrawCircle((int)circleX, (int)(sliderPosY + sliderHeight / 2), 10, Color.GREEN);

                // Draw the four difficulty levels
                for (int i = 0; i < 4; i++)
                {
                    Color color = (i * 2 + 1 == value) ? Color.GREEN : Color.DARKGRAY;
                    Raylib.DrawRectangle((int)(sliderPosX + i * sectionWidth), (int)sliderPosY, (int)sectionWidth, (int)sliderHeight, color);
                }
            }
        }
    }
}
