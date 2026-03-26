# Qwen3-TTS Benchmark — Consequences FYP

## Test Environment

| | |
|---|---|
| **Date** | 2026-03-21 18:13:36 |
| **Machine** | Ivans-MacBook-Air.local |
| **Platform** | macOS 26.3, Apple Silicon (arm64) |
| **Python** | 3.14.2 |
| **PyTorch** | 2.10.0 |
| **Device** | CPU (no CUDA/GPU available) |
| **Model** | Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice |
| **Precision** | float32 |
| **Voice** | vivian |

## Results

| Text | Words | Generation Time | Audio Length | Real-Time Factor |
|------|-------|----------------|--------------|-----------------|
| "Good job!" | 2 | **2.42s** | 1.12s | 2.2x |
| "Normal week, buy what you need!" | 6 | **6.53s** | 3.12s | 2.1x |
| "Save a little each week so surprises do not turn into debt. Goal: 160 pounds." | 15 | **15.67s** | 7.36s | 2.1x |
| "Round 1 of 3. Normal Week. Budget: 8 pounds 50. Items: Bread..." | 27 | **43.92s** | 19.84s | 2.2x |

> **RTF (Real-Time Factor):** 1.0x = audio generates as fast as it plays. Values above 1.0x mean the user must wait. Qwen3-TTS runs at ~2.1-2.2x on Apple Silicon CPU.

## Key Findings

- Even a **2-word phrase** ("Good job!") takes **2.4 seconds** to generate
- A **full sentence** (15 words) takes **15.7 seconds**
- Generation time scales linearly with text length at ~2x real-time
- No GPU acceleration available on this hardware (Apple Silicon, no CUDA)
- `flash-attn` requires NVIDIA GPU — not applicable here

## Comparison with Piper TTS

| | Qwen3-TTS 0.6B | Piper TTS |
|---|---|---|
| "Good job!" generation | **2.42s** | **~0.02s** |
| Model size | ~2.3 GB | ~30 MB |
| Runs offline | Yes | Yes |
| Unity integration | Complex (9 sub-models) | Proven plugin (Sentis) |
| Voice quality | Excellent | Good |
| Speed on CPU | 2.1-2.2x RTF (slower than real-time) | Real-time or faster |

**Piper TTS is ~100-200x faster on the same hardware.**

## Audio Samples

Generated audio samples are available in the `TTS/` folder:
- `bench_short.wav` — "Good job!"
- `bench_medium.wav` — "Normal week, buy what you need!"
- `bench_long.wav` — Full sentence (15 words)
- `bench_very_long.wav` — Shopping list (27 words)

## Raw Terminal Output

The full unedited terminal output is preserved in `benchmark_results.txt`.

## Recommendation

Qwen3-TTS produces excellent voice quality but is too slow for real-time on-device use without a GPU. For an offline, snappy TTS experience in the game, **Piper TTS** is the practical choice — instant generation, tiny model, proven Unity integration.

Qwen3-TTS remains viable if hosted on a **server with GPU** (e.g. MotionInput infrastructure), where it achieves ~97ms first-packet latency.
