**Subject:** Qwen3-TTS Research — Need Your Input

Hi Dean,

I looked into Qwen3-TTS as you suggested. Here's where I am:

### What I Built

- TTS button across all screens — reads the duck mascot's messages aloud
- Integrated Qwen3-TTS 0.6B (vivian voice) via a local server, tested end-to-end in-game — voice quality is good

### The Problem: Speed on CPU

I benchmarked it on my MacBook Air (Apple Silicon, CPU, FP32):

| Text | Generation Time |
|------|----------------|
| "Good job!" (2 words) | **2.42s** |
| 6-word sentence | **6.53s** |
| 15-word sentence | **15.67s** |

Even a 2-word phrase has a 2.4s delay before any audio plays. ONNX Runtime would be slightly faster than PyTorch (~1.8s instead of 2.4s) but not meaningfully different — the bottleneck is 0.6B parameters doing autoregressive generation where each token depends on the previous one, so it can't be parallelized. On Windows without a GPU it would be even slower.

### Options I Explored

| Option | Issue |
|--------|-------|
| Unity Sentis | Not feasible (unsupported operators, 9 sub-models) |
| ONNX Runtime | Works but still ~2.4s+ per phrase on CPU |
| PyInstaller bundle | Works but fragile for users (subprocess, port conflicts) |
| MLX (Apple GPU) | Faster with streaming, but Apple-only |
| NPU / Neural Engine | Wrong workload for autoregressive generation |

### Alternative: Piper TTS

| | Qwen3-TTS | Piper TTS |
|---|---|---|
| "Good job!" | 2.42s | **~0.02s** |
| Model size | 2+ GB | 30 MB |
| Voice quality | Excellent | Good |
| Offline | Yes | Yes |

### My Thinking

I want TTS fully offline since it's an accessibility feature. Qwen3-TTS sounds great but 2.4s delay is too slow for a responsive UI. Piper is instant but lower quality.

One option: use Piper for offline, and if MotionInput has a server, host Qwen3-TTS there for users with internet — the game would just point to that server URL instead of localhost.

I have audio samples and detailed research in the repo. Happy to discuss — when works for you?

Best,
Ivan
