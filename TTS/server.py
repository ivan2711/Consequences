"""
Qwen3-TTS server for Consequences game.
Accepts text via HTTP, returns WAV audio.

When bundled: model files sit in a 'model/' folder next to this script.
When developing: downloads from HuggingFace on first run.
"""

import io
import os
import sys
import threading
import torch
import soundfile as sf
from fastapi import FastAPI, Query
from fastapi.responses import StreamingResponse, JSONResponse
from qwen_tts import Qwen3TTSModel

# Determine model path
if getattr(sys, 'frozen', False):
    BASE_DIR = os.path.dirname(sys.executable)
else:
    BASE_DIR = os.path.dirname(os.path.abspath(__file__))

LOCAL_MODEL = os.path.join(BASE_DIR, "model")
MODEL_NAME = LOCAL_MODEL if os.path.isdir(LOCAL_MODEL) else "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice"

DEVICE = "cuda:0" if torch.cuda.is_available() else "cpu"
DTYPE = torch.bfloat16 if torch.cuda.is_available() else torch.float32

app = FastAPI(title="Consequences TTS Server")

SPEAKER = "vivian"

# Lazy model loading in background thread
model = None
model_ready = False
model_error = None


def load_model():
    global model, model_ready, model_error
    try:
        print(f"Loading {MODEL_NAME} on {DEVICE}...")
        attn_impl = "flash_attention_2" if torch.cuda.is_available() else "eager"
        model = Qwen3TTSModel.from_pretrained(
            MODEL_NAME,
            device_map=DEVICE,
            dtype=DTYPE,
            attn_implementation=attn_impl,
        )
        model_ready = True
        print("Model loaded.")
    except Exception as e:
        model_error = str(e)
        print(f"Model load failed: {e}")


# Start loading in background thread
threading.Thread(target=load_model, daemon=True).start()


@app.get("/tts")
async def text_to_speech(
    text: str = Query(..., description="Text to speak"),
    speaker: str = Query(SPEAKER, description="Speaker voice name"),
):
    if not model_ready:
        return JSONResponse(
            status_code=503,
            content={"error": "Model still loading, try again in a few seconds"}
        )

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
    return {
        "status": "ready" if model_ready else "loading",
        "model": MODEL_NAME,
        "device": str(DEVICE),
        "error": model_error,
    }


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="127.0.0.1", port=7860)
