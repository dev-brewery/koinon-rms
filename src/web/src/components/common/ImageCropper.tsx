/**
 * ImageCropper Component
 * Canvas-based image cropping with 1:1 aspect ratio for profile photos
 */

import { useState, useRef, useEffect } from 'react';
import { cn } from '@/lib/utils';

export interface ImageCropperProps {
  imageSrc: string;
  onCropComplete: (blob: Blob) => void;
  onCancel: () => void;
}

interface CropArea {
  x: number;
  y: number;
  size: number;
}

export function ImageCropper({ imageSrc, onCropComplete, onCancel }: ImageCropperProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [image, setImage] = useState<HTMLImageElement | null>(null);
  const [cropArea, setCropArea] = useState<CropArea | null>(null);
  const [isDragging, setIsDragging] = useState(false);
  const [dragStart, setDragStart] = useState<{ x: number; y: number } | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [cropError, setCropError] = useState<string | null>(null);

  // Load image with cancellation to prevent setState on unmounted component
  useEffect(() => {
    const img = new Image();
    let cancelled = false;

    img.onload = () => {
      if (!cancelled) {
        setImage(img);
        // Initialize crop area to center square
        const size = Math.min(img.width, img.height);
        const x = (img.width - size) / 2;
        const y = (img.height - size) / 2;
        setCropArea({ x, y, size });
      }
    };

    img.onerror = () => {
      if (!cancelled) {
        setCropError('Failed to load image. Please try again.');
      }
    };

    img.src = imageSrc;

    return () => {
      cancelled = true;
    };
  }, [imageSrc]);

  // Draw canvas
  useEffect(() => {
    if (!image || !cropArea || !canvasRef.current) return;

    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    // Set canvas size to fit container
    const maxWidth = 600;
    const maxHeight = 600;
    const scale = Math.min(maxWidth / image.width, maxHeight / image.height);

    canvas.width = image.width * scale;
    canvas.height = image.height * scale;

    // Draw image
    ctx.drawImage(image, 0, 0, canvas.width, canvas.height);

    // Draw overlay
    ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Clear crop area
    const scaledX = cropArea.x * scale;
    const scaledY = cropArea.y * scale;
    const scaledSize = cropArea.size * scale;

    ctx.clearRect(scaledX, scaledY, scaledSize, scaledSize);
    ctx.drawImage(
      image,
      cropArea.x,
      cropArea.y,
      cropArea.size,
      cropArea.size,
      scaledX,
      scaledY,
      scaledSize,
      scaledSize
    );

    // Draw crop border
    ctx.strokeStyle = '#3b82f6';
    ctx.lineWidth = 2;
    ctx.strokeRect(scaledX, scaledY, scaledSize, scaledSize);

    // Draw corner handles
    const handleSize = 12;
    ctx.fillStyle = '#3b82f6';
    // Top-left
    ctx.fillRect(scaledX - handleSize / 2, scaledY - handleSize / 2, handleSize, handleSize);
    // Top-right
    ctx.fillRect(
      scaledX + scaledSize - handleSize / 2,
      scaledY - handleSize / 2,
      handleSize,
      handleSize
    );
    // Bottom-left
    ctx.fillRect(
      scaledX - handleSize / 2,
      scaledY + scaledSize - handleSize / 2,
      handleSize,
      handleSize
    );
    // Bottom-right
    ctx.fillRect(
      scaledX + scaledSize - handleSize / 2,
      scaledY + scaledSize - handleSize / 2,
      handleSize,
      handleSize
    );
  }, [image, cropArea]);

  const handleMouseDown = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!canvasRef.current || !cropArea || !image) return;

    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const scale = canvas.width / rect.width;
    const x = (e.clientX - rect.left) * scale;
    const y = (e.clientY - rect.top) * scale;

    const imageScale = canvas.width / image.width;
    const scaledX = cropArea.x * imageScale;
    const scaledY = cropArea.y * imageScale;
    const scaledSize = cropArea.size * imageScale;

    // Check if click is inside crop area
    if (
      x >= scaledX &&
      x <= scaledX + scaledSize &&
      y >= scaledY &&
      y <= scaledY + scaledSize
    ) {
      setIsDragging(true);
      setDragStart({ x: x - scaledX, y: y - scaledY });
    }
  };

  const handleMouseMove = (e: React.MouseEvent<HTMLCanvasElement>) => {
    if (!isDragging || !dragStart || !canvasRef.current || !cropArea || !image) return;

    const canvas = canvasRef.current;
    const rect = canvas.getBoundingClientRect();
    const scale = canvas.width / rect.width;
    const x = (e.clientX - rect.left) * scale;
    const y = (e.clientY - rect.top) * scale;

    const imageScale = canvas.width / image.width;
    let newX = (x - dragStart.x) / imageScale;
    let newY = (y - dragStart.y) / imageScale;

    // Constrain to image bounds
    newX = Math.max(0, Math.min(newX, image.width - cropArea.size));
    newY = Math.max(0, Math.min(newY, image.height - cropArea.size));

    setCropArea({ ...cropArea, x: newX, y: newY });
  };

  const handleMouseUp = () => {
    setIsDragging(false);
    setDragStart(null);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLCanvasElement>) => {
    if (!cropArea || !image) return;

    const step = 10; // pixels to move per keypress
    let newX = cropArea.x;
    let newY = cropArea.y;

    switch (e.key) {
      case 'ArrowLeft':
        newX = Math.max(0, cropArea.x - step);
        break;
      case 'ArrowRight':
        newX = Math.min(image.width - cropArea.size, cropArea.x + step);
        break;
      case 'ArrowUp':
        newY = Math.max(0, cropArea.y - step);
        break;
      case 'ArrowDown':
        newY = Math.min(image.height - cropArea.size, cropArea.y + step);
        break;
      case 'Enter':
        e.preventDefault();
        handleCrop();
        return;
      default:
        return;
    }

    e.preventDefault();
    setCropArea({ ...cropArea, x: newX, y: newY });
  };

  const handleCrop = async () => {
    if (!image || !cropArea) return;

    setIsProcessing(true);
    setCropError(null);

    try {
      // Create a temporary canvas for cropping
      const tempCanvas = document.createElement('canvas');
      const targetSize = 512; // Output size for profile photo
      tempCanvas.width = targetSize;
      tempCanvas.height = targetSize;

      const ctx = tempCanvas.getContext('2d');
      if (!ctx) {
        throw new Error('Failed to get canvas context');
      }

      // Draw cropped image
      ctx.drawImage(
        image,
        cropArea.x,
        cropArea.y,
        cropArea.size,
        cropArea.size,
        0,
        0,
        targetSize,
        targetSize
      );

      // Convert to blob
      tempCanvas.toBlob((blob) => {
        if (blob) {
          onCropComplete(blob);
        } else {
          setCropError('Failed to crop image. Please try again.');
        }
        setIsProcessing(false);
      }, 'image/jpeg', 0.9);
    } catch (error) {
      setIsProcessing(false);
      setCropError('Failed to crop image. Please try again.');
      console.error('Crop failed:', error);
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
        {/* Backdrop */}
        <div
          className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
          onClick={onCancel}
        />

        {/* Modal */}
        <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full">
          <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
            <div className="flex items-start justify-between mb-4">
              <h3 className="text-lg font-medium text-gray-900">Crop Photo</h3>
              <button
                onClick={onCancel}
                className="text-gray-400 hover:text-gray-500"
                aria-label="Close"
              >
                <svg
                  className="w-6 h-6"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                  aria-hidden="true"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth={2}
                    d="M6 18L18 6M6 6l12 12"
                  />
                </svg>
              </button>
            </div>

            {/* Canvas */}
            <div className="flex items-center justify-center bg-gray-100 rounded-lg p-4">
              <canvas
                ref={canvasRef}
                onMouseDown={handleMouseDown}
                onMouseMove={handleMouseMove}
                onMouseUp={handleMouseUp}
                onMouseLeave={handleMouseUp}
                onKeyDown={handleKeyDown}
                tabIndex={0}
                role="application"
                aria-label="Photo crop area. Use mouse to drag or arrow keys to move crop area. Press Enter to confirm crop."
                className={cn(
                  'max-w-full max-h-[600px] cursor-move focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
                  isDragging ? 'cursor-grabbing' : 'cursor-grab'
                )}
              />
            </div>

            {cropError && (
              <div className="mt-2 p-3 bg-red-50 border border-red-200 rounded-lg">
                <p className="text-sm text-red-600">{cropError}</p>
              </div>
            )}

            <p className="mt-4 text-sm text-gray-600 text-center">
              Drag the crop area with your mouse or use arrow keys to adjust the photo
            </p>
          </div>

          {/* Actions */}
          <div className="bg-gray-50 px-4 py-3 sm:px-6 sm:flex sm:flex-row-reverse gap-3">
            <button
              onClick={handleCrop}
              disabled={isProcessing}
              className="w-full sm:w-auto px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {isProcessing ? 'Processing...' : 'Crop & Save'}
            </button>
            <button
              onClick={onCancel}
              disabled={isProcessing}
              className="w-full sm:w-auto mt-3 sm:mt-0 px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 disabled:opacity-50 transition-colors"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
