"""
This script imports all the packages used in the backend
and performs some basic tests.
"""

import sys
from importlib.metadata import version
print('Python:', sys.version)
print()

# Test core packages
import aiohttp
print('aiohttp:', aiohttp.__version__)
print('Flask:', version('flask'))

import flask_cors
print('flask-cors:', flask_cors.__version__)
print('langchain-core:', version('langchain-core'))
print('langchain-openai:', version('langchain-openai'))

import jwt
print('PyJWT:', jwt.__version__)
print('python-dotenv:', version('python-dotenv'))

import requests
print('requests:', requests.__version__)

import tiktoken
print('tiktoken:', tiktoken.__version__)

# Test tiktoken encoding_for_model (critical function used in sql_generator.py)
print()
print('Testing tiktoken.encoding_for_model...')
enc = tiktoken.encoding_for_model('gpt-4o-mini')
tokens = enc.encode('Hello world!')
print(f'Encoded to {len(tokens)} tokens: {tokens}')
print('SUCCESS: tiktoken works correctly!')

# Test JWT encode/decode (critical functions used in auth.py)
print()
print('Testing PyJWT...')
secret = 'test_secret_key_that_is_long_enough_for_hmac_256_algorithm'
payload = {'user_id': 'test', 'tenant': 'default'}
token = jwt.encode(payload, secret, algorithm='HS256')
decoded = jwt.decode(token, secret, algorithms=['HS256'], options={'verify_aud': False, 'verify_iss': False})
print(f'Encoded payload: {payload}')
print(f'Decoded payload: {decoded}')
print('SUCCESS: PyJWT works correctly!')

# Test aiohttp async client (pattern used in sql_generator.py)
print()
print('Testing aiohttp...')
import asyncio
async def test_aiohttp():
    async with aiohttp.ClientSession() as session:
        print('ClientSession created successfully')
    return True
asyncio.run(test_aiohttp())
print('SUCCESS: aiohttp works correctly!')

print()
print('='*50)
print('ALL PACKAGES VERIFIED SUCCESSFULLY!')
print('='*50)