"""
Qwen3-TTS server for Consequences game.
Accepts text via HTTP, returns WAV audio.

Setup:
    pip install -U qwen-tts fastapi uvicorn soundfile
    pip install -U flash-attn --no-build-isolation  # optional, for faster GPU inference

Run:
    python server.py
    # or: uvicorn server:app --host 0.0.0.0 --port 7860
"""

import io
import torch
import soundfile as sf
from fastapi import FastAPI, Query
from fastapi.responses import StreamingResponse
from qwen_tts import Qwen3TTSModel

# Use the smaller 0.6B model for speed; swap to 1.7B for higher quality
MODEL_NAME = "Qwen/Qwen3-TTS-12Hz-0.6B"
DEVICE = "cuda:0" if torch.cuda.is_available() else "cpu"
DTYPE = torch.bfloat16 if torch.cuda.is_available() else torch.float32

print(f"Loading {MODEL_NAME} on {DEVICE}...")

attn_impl = "flash_attention_2" if torch.cuda.is_available() else "eager"

model = Qwen3TTSModel.from_pretrained(
    MODEL_NAME,
    device_map=DEVICE,
    dtype=DTYPE,
    attn_implementation=attn_impl,
)

print("Model loaded.")

app = FastAPI(title="Consequences TTS Server")

SPEAKER = "Chelsie"  # friendly English female voice


@app.get("/tts")
async def text_to_speech(
    text: str = Query(..., description="Text to speak"),
    speaker: str = Query(SPEAKER, description="Speaker voice name"),
    speed: float = Query(1.0, description="Speech speed multiplier"),
):
    """Generate speech from text, return WAV audio."""
    wavs, sr = model.generate_custom_voice(
        text=text,
        language="English",
        speaker=speaker,
    )

    buf = io.BytesIO()
    sf.write(buf, wavs[0], sr, format="WAV")
    buf.seek(0)

    return StreamingResponse(buf, media_type="audio/wav")


@app.get("/health")
async def health():
    return {"status": "ok", "model": MODEL_NAME, "device": str(DEVICE)}


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=7860)
