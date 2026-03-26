# Qwen3-TTS Integration Research

> **Date:** 2026-03-17
> **Context:** Dean suggested Qwen3-TTS. Researched feasibility of bundling it in-app via ONNX for offline use.

---

## 1. Model Sizes (Qwen3-TTS 0.6B)

### FP32 ONNX ([zukky/Qwen3-TTS-ONNX-DLL](https://huggingface.co/zukky/Qwen3-TTS-ONNX-DLL))
**Total: ~6.55 GB**

| File | Size |
|------|------|
| talker_prefill.onnx | 1.78 GB |
| talker_decode.onnx | 1.78 GB |
| text_project.onnx | 1.27 GB |
| tokenizer12hz_decode.onnx | 457 MB |
| code_predictor.onnx | 441 MB |
| tokenizer12hz_encode.onnx | 193 MB |
| code_predictor_embed.onnx | 126 MB |
| speaker_encoder.onnx | 35.6 MB |
| codec_embed.onnx | 12.6 MB |

### INT8 Quantized ([sivasub987/Qwen3-TTS-0.6B-ONNX-INT8](https://huggingface.co/sivasub987/Qwen3-TTS-0.6B-ONNX-INT8))
**Total: ~2.08 GB** (68% smaller)
- **BUT:** `speaker_encoder`, `tokenizer12hz_encode`, `tokenizer12hz_decode` FAIL on ONNX Runtime due to missing `ConvInteger` operator
- Need to mix with FP32 audio decoder = ~2.5 GB effective

### GGUF Q5_K_M ([cgisky/qwen3-tts-custom-gguf](https://huggingface.co/cgisky/qwen3-tts-custom-gguf))
**Total: ~1.45 GB** (smallest working)
- Needs GGUF runtime, not standard ONNX Runtime

---

## 2. Architecture

9 sub-models in a pipeline:

```
text_project -> speaker_encoder -> talker_prefill -> talker_decode (autoregressive loop)
  -> code_predictor (autoregressive, 15 codebook groups) -> tokenizer12hz_decode -> audio
```

- 28 transformer layers, 16 attention heads, hidden_dim=1024
- 16 RVQ codebook groups at 12 Hz frame rate
- Output: 24 kHz mono WAV
- Autoregressive = must run sequentially, cannot be parallelised

---

## 3. Inference Speed — Measured on Our Hardware

**Device:** Apple Silicon CPU (M-series Mac), no GPU
**Model:** Qwen3-TTS-12Hz-0.6B-CustomVoice, FP32, PyTorch
**Voice:** vivian

| Text | Words | Generation Time | Audio Length | RTF |
|------|-------|----------------|--------------|-----|
| "Good job!" | 2 | **2.42s** | 1.12s | 2.2x |
| "Normal week, buy what you need!" | 6 | **6.53s** | 3.12s | 2.1x |
| "Save a little each week..." | 15 | **15.67s** | 7.36s | 2.1x |
| "Round 1 of 3. Normal Week..." | 27 | **43.92s** | 19.84s | 2.2x |

> **RTF = Real-Time Factor.** 1.0x = generates as fast as it plays. Higher = slower.
> Qwen3-TTS runs at ~2.1-2.2x slower than real-time on CPU.
>
> Even a **2-word phrase takes 2.4 seconds.** For comparison, Piper TTS generates in **20-30 milliseconds** on the same CPU.

Official GPU benchmarks (for reference):
- First-packet latency: ~97 ms
- Real-time factor: 0.23-0.31

---

## 4. Unity Integration Options

### Option A: Unity Sentis (ONNX inside Unity)
- **NOT FEASIBLE:** Sentis doesn't support required operators (`If`, etc.)
- 9 sub-models with autoregressive loops too complex for Sentis
- Nobody has done this

### Option B: ONNX Runtime in Unity ([asus4/onnxruntime-unity](https://github.com/asus4/onnxruntime-unity))
- Feasible but heavy porting work
- [ElBruno/ElBruno.QwenTTS](https://github.com/elbruno/ElBruno.QwenTTS) has full C# pipeline (but .NET 10, needs porting to Unity C# 9)
- Supports: Windows, macOS, iOS, Android
- GPU: CoreML (macOS/iOS), DirectML (Windows), CUDA

### Option C: Bundle Python server as executable (PyInstaller)
- Simplest but worst UX (subprocess, firewall prompts, port conflicts)

### Option D: Use Piper TTS instead
- 30 MB model, runs in 20-30ms on CPU
- Proven Unity integration ([Macoron/piper.unity](https://github.com/Macoron/piper.unity) via Sentis)
- Lower voice quality than Qwen3-TTS
- Also: [Unity's official Jets TTS sample](https://huggingface.co/unity/inference-engine-jets-text-to-speech) (~30 MB)

---

## 5. Comparison Table

| | Qwen3-TTS 0.6B | Piper TTS | Unity Jets TTS |
|---|---|---|---|
| **Model size** | 2-6.5 GB | 20-60 MB | ~30 MB |
| **CPU inference** | 2-44 sec | 20-30 ms | real-time |
| **Unity integration** | Complex port | Proven plugin | Official sample |
| **Voice quality** | Excellent | Good | Good |
| **Offline** | Yes | Yes | Yes |

---

## 6. Why the Size Varies

The native PyTorch model (BF16) is only **1.83 GB** — Dean's suggestion was sound.

The 6.5 GB ONNX FP32 figure is the **worst case**, caused by:
- **BF16 (16-bit) to FP32 (32-bit):** doubles weight size (1.83 to ~3.66 GB)
- **Prefill/decode split:** `talker_prefill.onnx` and `talker_decode.onnx` contain the **same transformer weights** duplicated in two separate graphs (+1.78 GB)

More realistic sizes:
| Format | Size | Notes |
|--------|------|-------|
| ONNX INT8 | ~2 GB | Comparable to original, but some components broken |
| ONNX FP16 | ~3.3 GB | No published version yet |
| ONNX FP32 | ~6.5 GB | Worst case, weight duplication + format expansion |

The real issue isn't the format — it's that a 0.6B parameter model is inherently large and slow on CPU regardless of format. Plus the 9-model pipeline is complex to orchestrate in Unity C#.

---

## 7. Sources

### HuggingFace
- https://huggingface.co/Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice
- https://huggingface.co/sivasub987/Qwen3-TTS-0.6B-ONNX-INT8
- https://huggingface.co/zukky/Qwen3-TTS-ONNX-DLL
- https://huggingface.co/cgisky/qwen3-tts-custom-gguf
- https://huggingface.co/unity/inference-engine-jets-text-to-speech

### GitHub
- https://github.com/QwenLM/Qwen3-TTS
- https://github.com/elbruno/ElBruno.QwenTTS (C# ONNX pipeline)
- https://github.com/asus4/onnxruntime-unity (ONNX Runtime Unity plugin)
- https://github.com/Macoron/piper.unity (Piper TTS Unity)

---

## 8. Current State in Project

- TTS button + TTSManager: **DONE** (works across all scenes)
- DuckReaction sets TTS content on every message
- TTSService.cs: HTTP client to localhost:7860
- TTS/server.py: FastAPI server wrapping Qwen3-TTS (working, tested)
- TTSServerLauncher.cs: auto-launches server from Unity editor
- Backend decision pending discussion with Dean

**Git tag:** `stable-before-onnx` (restore point)
