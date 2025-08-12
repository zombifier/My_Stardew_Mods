using System;
using StardewValley;
using StardewValley.Extensions;

namespace Selph.StardewMods.RageBait;

// I just copy pastaed this entire thing from BobberBar lmao
class DecoyBobber {
  public float bobberPosition;
  public float bobberSpeed;
  public float bobberAcceleration;
  public float bobberTargetPosition;
  public float difficulty;
  public int motionType;
  public float floaterSinkerAcceleration;

  public DecoyBobber(float bobberPosition, float difficulty, int motionType) {
    this.bobberPosition = bobberPosition;
    this.difficulty = difficulty;
    this.motionType = motionType;
    this.bobberTargetPosition = 568 - bobberPosition;
  }

  public void update() {
    if (Game1.random.NextDouble() < (double)(this.difficulty * (float)((this.motionType != 2) ? 1 : 20) / 4000f) && (this.motionType != 2 || this.bobberTargetPosition == -1f)) {
      float spaceBelow = 548f - this.bobberPosition;
      float spaceAbove = this.bobberPosition;
      float percent = Math.Min(99f, this.difficulty + (float)Game1.random.Next(10, 45)) / 100f;
      this.bobberTargetPosition = this.bobberPosition + (float)Game1.random.Next((int)Math.Min(0f - spaceAbove, spaceBelow), (int)spaceBelow) * percent;
    }
    switch (this.motionType) {
      case 4:
        this.floaterSinkerAcceleration = Math.Max(this.floaterSinkerAcceleration - 0.01f, -1.5f);
        break;
      case 3:
        this.floaterSinkerAcceleration = Math.Min(this.floaterSinkerAcceleration + 0.01f, 1.5f);
        break;
    }
    if (Math.Abs(this.bobberPosition - this.bobberTargetPosition) > 3f && this.bobberTargetPosition != -1f) {
      this.bobberAcceleration = (this.bobberTargetPosition - this.bobberPosition) / ((float)Game1.random.Next(10, 30) + (100f - Math.Min(100f, this.difficulty)));
      this.bobberSpeed += (this.bobberAcceleration - this.bobberSpeed) / 5f;
    } else if (this.motionType != 2 && Game1.random.NextDouble() < (double)(this.difficulty / 2000f)) {
      this.bobberTargetPosition = this.bobberPosition + (float)(Game1.random.NextBool() ? Game1.random.Next(-100, -51) : Game1.random.Next(50, 101));
    } else {
      this.bobberTargetPosition = -1f;
    }
    if (this.motionType == 1 && Game1.random.NextDouble() < (double)(this.difficulty / 1000f)) {
      this.bobberTargetPosition = this.bobberPosition + (float)(Game1.random.NextBool() ? SafeNext(Game1.random, -100 - (int)this.difficulty * 2, -51) : SafeNext(Game1.random, 50, 101 + (int)this.difficulty * 2));
    }
    this.bobberTargetPosition = Math.Max(-1f, Math.Min(this.bobberTargetPosition, 548f));
    this.bobberPosition += this.bobberSpeed + this.floaterSinkerAcceleration;
    if (this.bobberPosition > 532f) {
      this.bobberPosition = 532f;
    } else if (this.bobberPosition < 0f) {
      this.bobberPosition = 0f;
    }
  }

  private static int SafeNext(Random random, int minValue, int maxValue) {
    if (minValue >= maxValue) {
      return maxValue;
    }
    return random.Next(minValue, maxValue);
  }
}
