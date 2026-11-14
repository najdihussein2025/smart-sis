from functools import lru_cache

from pydantic_settings import BaseSettings, SettingsConfigDict

DEFAULT_MODEL = "claude-opus-5-5"


class Settings(BaseSettings):
    """Read from environment / .env. Never hardcode keys."""

    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    service_key: str = ""
    sis_api_base_url: str = "http://localhost:5080"
    # Anthropic API key. Empty = /chat answers 503 and the .NET API shows "not set up yet".
    llm_api_key: str = ""
    # Empty means DEFAULT_MODEL.
    llm_model: str = ""
    # output_config.effort: low | medium | high | xhigh | max.
    llm_effort: str = "medium"
    llm_max_tokens: int = 16000
    # The model may call tools a few times before it answers; stop after this many rounds.
    llm_max_tool_rounds: int = 6
    # KnowledgeChunks only (write on /ingest, read when searching).
    sql_connection_string: str = ""

    @property
    def model(self) -> str:
        return self.llm_model.strip() or DEFAULT_MODEL


@lru_cache
def get_settings() -> Settings:
    return Settings()
