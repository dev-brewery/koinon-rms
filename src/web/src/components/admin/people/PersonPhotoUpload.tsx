/**
 * PersonPhotoUpload Component
 * Complete photo upload flow for person profiles
 */

import { useState, useEffect } from 'react';
import { PhotoUpload } from '@/components/common/PhotoUpload';
import { ImageCropper } from '@/components/common/ImageCropper';
import { useUploadPersonPhoto } from '@/hooks/usePeople';

export interface PersonPhotoUploadProps {
  personIdKey: string;
  currentPhotoUrl?: string | null;
}

export function PersonPhotoUpload({
  personIdKey,
  currentPhotoUrl,
}: PersonPhotoUploadProps) {
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [imageSrc, setImageSrc] = useState<string | null>(null);
  const [showCropper, setShowCropper] = useState(false);
  const [showUploadModal, setShowUploadModal] = useState(false);

  const uploadMutation = useUploadPersonPhoto();

  const handleFileSelect = (file: File) => {
    setSelectedFile(file);

    // Create image URL for cropper using object URL (memory efficient)
    const objectUrl = URL.createObjectURL(file);
    setImageSrc(objectUrl);
    setShowCropper(true);
  };

  // Cleanup image URL on unmount or when imageSrc changes
  useEffect(() => {
    return () => {
      if (imageSrc) {
        URL.revokeObjectURL(imageSrc);
      }
    };
  }, [imageSrc]);

  const handleCropComplete = async (blob: Blob) => {
    // Convert blob to File
    const file = new File([blob], selectedFile?.name || 'photo.jpg', {
      type: 'image/jpeg',
    });

    try {
      await uploadMutation.mutateAsync({ idKey: personIdKey, file });
      // Cleanup before clearing
      if (imageSrc) {
        URL.revokeObjectURL(imageSrc);
      }
      setShowCropper(false);
      setShowUploadModal(false);
      setSelectedFile(null);
      setImageSrc(null);
    } catch (error) {
      // Error is handled by mutation
    }
  };

  const handleCancelCrop = () => {
    // Cleanup before clearing
    if (imageSrc) {
      URL.revokeObjectURL(imageSrc);
    }
    setShowCropper(false);
    setSelectedFile(null);
    setImageSrc(null);
  };

  const handleCloseUploadModal = () => {
    // Cleanup before clearing
    if (imageSrc) {
      URL.revokeObjectURL(imageSrc);
    }
    setShowUploadModal(false);
    setSelectedFile(null);
    setImageSrc(null);
  };

  return (
    <div className="space-y-4">
      {/* Current Photo Display */}
      <div className="flex items-center gap-6">
        <div className="relative">
          {currentPhotoUrl ? (
            <img
              src={currentPhotoUrl}
              alt="Profile"
              className="w-32 h-32 rounded-full object-cover border-4 border-gray-200"
            />
          ) : (
            <div className="w-32 h-32 rounded-full bg-gray-200 flex items-center justify-center border-4 border-gray-200">
              <svg
                className="w-16 h-16 text-gray-400"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z"
                />
              </svg>
            </div>
          )}

          {uploadMutation.isPending && (
            <div className="absolute inset-0 bg-white bg-opacity-75 rounded-full flex items-center justify-center">
              <svg
                className="animate-spin h-8 w-8 text-blue-600"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                />
              </svg>
            </div>
          )}
        </div>

        <div className="flex-1">
          <button
            onClick={() => setShowUploadModal(true)}
            disabled={uploadMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors font-medium"
          >
            {currentPhotoUrl ? 'Change Photo' : 'Upload Photo'}
          </button>

          {uploadMutation.isSuccess && (
            <p className="mt-2 text-sm text-green-600">Photo uploaded successfully!</p>
          )}

          {uploadMutation.isError && (
            <p className="mt-2 text-sm text-red-600">
              Failed to upload photo. Please try again.
            </p>
          )}
        </div>
      </div>

      {/* Upload Modal */}
      {showUploadModal && !showCropper && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-center justify-center min-h-screen px-4 pt-4 pb-20 text-center sm:p-0">
            {/* Backdrop */}
            <div
              className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity"
              onClick={handleCloseUploadModal}
            />

            {/* Modal */}
            <div className="relative inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full">
              <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
                <div className="flex items-start justify-between mb-4">
                  <h3 className="text-lg font-medium text-gray-900">Upload Photo</h3>
                  <button
                    onClick={handleCloseUploadModal}
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

                <PhotoUpload onFileSelect={handleFileSelect} />

                <p className="mt-4 text-sm text-gray-600">
                  After selecting a photo, you'll be able to crop it to fit.
                </p>
              </div>

              <div className="bg-gray-50 px-4 py-3 sm:px-6">
                <button
                  onClick={handleCloseUploadModal}
                  className="w-full px-4 py-2 bg-white border border-gray-300 text-gray-700 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Image Cropper Modal */}
      {showCropper && imageSrc && (
        <ImageCropper
          imageSrc={imageSrc}
          onCropComplete={handleCropComplete}
          onCancel={handleCancelCrop}
        />
      )}
    </div>
  );
}
