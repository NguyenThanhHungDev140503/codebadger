"""
Credential Encryption Adapter Interface and Implementations.
"""

import base64
import os
from abc import ABC, abstractmethod
from typing import Optional


class CredentialEncryptionAdapter(ABC):
    """Abstract interface for encrypting and decrypting sensitive credentials."""

    @abstractmethod
    def encrypt(self, plaintext: str) -> str:
        """Encrypt plaintext secret and return ciphertext token/envelope."""
        pass

    @abstractmethod
    def decrypt(self, ciphertext: str) -> str:
        """Decrypt ciphertext token/envelope and return plaintext secret."""
        pass


class InMemoryCredentialEncryptionAdapter(CredentialEncryptionAdapter):
    """Simple XOR/Base64 in-memory encryption adapter for testing."""

    def __init__(self, key: str = "test-secret-key"):
        self.key = key.encode("utf-8")

    def encrypt(self, plaintext: str) -> str:
        data = plaintext.encode("utf-8")
        encrypted = bytes([b ^ self.key[i % len(self.key)] for i, b in enumerate(data)])
        return base64.b64encode(encrypted).decode("utf-8")

    def decrypt(self, ciphertext: str) -> str:
        data = base64.b64decode(ciphertext.encode("utf-8"))
        decrypted = bytes([b ^ self.key[i % len(self.key)] for i, b in enumerate(data)])
        return decrypted.decode("utf-8")


class FernetCredentialEncryptionAdapter(CredentialEncryptionAdapter):
    """Production credential encryption adapter using cryptography.fernet."""

    def __init__(self, secret_key: Optional[str] = None):
        key = secret_key or os.getenv("CODEBADGER_CREDENTIAL_KEY")
        if not key:
            # Fall back to deterministic derived key for dev/test if env var missing
            key = base64.urlsafe_b64encode(b"codebadger-default-32-byte-key!!")
        else:
            if isinstance(key, str):
                key = key.encode("utf-8")
            if len(key) != 44:
                # If key is raw string instead of fernet key, b64encode it
                key = base64.urlsafe_b64encode(key.ljust(32)[:32])

        from cryptography.fernet import Fernet
        self.fernet = Fernet(key)

    def encrypt(self, plaintext: str) -> str:
        return self.fernet.encrypt(plaintext.encode("utf-8")).decode("utf-8")

    def decrypt(self, ciphertext: str) -> str:
        return self.fernet.decrypt(ciphertext.encode("utf-8")).decode("utf-8")
