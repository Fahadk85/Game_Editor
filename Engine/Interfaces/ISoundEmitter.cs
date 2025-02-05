﻿namespace Editor.Engine.Interfaces
{
    enum SoundEffectTypes
    {
        OnSelect = 0,
        OnTakeDamage = 1
    };

    internal interface ISoundEmitter
    {
        public SFXInstance[] SoundEffects { get; }
    }
}
