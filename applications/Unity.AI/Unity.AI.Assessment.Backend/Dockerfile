FROM python:3.11-slim

WORKDIR /app

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the source code
COPY src/ .

# Expose port
EXPOSE 5000

# Run the application
CMD ["python", "app.py"]