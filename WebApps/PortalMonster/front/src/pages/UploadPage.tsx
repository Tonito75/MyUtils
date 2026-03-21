import { useState, useRef } from 'react'
import { Box, Button, Typography, Paper, CircularProgress, Alert, Stack } from '@mui/material'
import CameraAltIcon from '@mui/icons-material/CameraAlt'
import { useNavigate } from 'react-router-dom'
import { analyzePhoto, publishPhoto, type AnalyzeResult } from '../api/photos'

type Step = 'select' | 'detected' | 'error'

export default function UploadPage() {
  const [step, setStep] = useState<Step>('select')
  const [file, setFile] = useState<File | null>(null)
  const [preview, setPreview] = useState<string | null>(null)
  const [analyzing, setAnalyzing] = useState(false)
  const [publishing, setPublishing] = useState(false)
  const [result, setResult] = useState<AnalyzeResult | null>(null)
  const inputRef = useRef<HTMLInputElement>(null)
  const navigate = useNavigate()

  const handleFile = async (f: File) => {
    setFile(f)
    setPreview(URL.createObjectURL(f))
    setAnalyzing(true)
    setStep('select')
    try {
      const res = await analyzePhoto(f)
      setResult(res.data)
      setStep('detected')
    } catch (err: any) {
      setStep('error')
    } finally {
      setAnalyzing(false)
    }
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const f = e.target.files?.[0]
    if (f) handleFile(f)
  }

  const handlePublish = async () => {
    if (!file || !result) return
    setPublishing(true)
    try {
      await publishPhoto(file, result.monsterId)
      navigate('/')
    } catch {
      /* show error */
    } finally {
      setPublishing(false)
    }
  }

  const handleRetry = () => {
    setStep('select')
    setFile(null)
    setPreview(null)
    setResult(null)
    if (inputRef.current) inputRef.current.value = ''
  }

  return (
    <Box sx={{ maxWidth: 480, mx: 'auto' }}>
      <Typography variant="h6" mb={2} fontWeight={700}>Publier une photo</Typography>

      <Paper sx={{ p: 3 }}>
        {/* Step 1: Select */}
        {step === 'select' && !analyzing && (
          <Box sx={{ textAlign: 'center' }}>
            <CameraAltIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
            <Typography color="text.secondary" mb={2}>
              Prends une photo de ta canette Monster
            </Typography>
            <Button variant="contained" onClick={() => inputRef.current?.click()}>
              Choisir une photo
            </Button>
            <input ref={inputRef} type="file" accept="image/*"
              style={{ display: 'none' }} onChange={handleInputChange} />
          </Box>
        )}

        {/* Analyzing */}
        {analyzing && (
          <Box sx={{ textAlign: 'center', py: 4 }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <CircularProgress />
            <Typography mt={2} color="text.secondary">Identification en cours...</Typography>
          </Box>
        )}

        {/* Step 2a: Detected */}
        {step === 'detected' && result && (
          <Box sx={{ textAlign: 'center' }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <Alert severity="success" sx={{ mb: 2 }}>
              Monster détecté : <strong>{result.emoji} {result.monsterName}</strong>
            </Alert>
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button variant="outlined" onClick={handleRetry}>Reprendre</Button>
              <Button variant="contained" onClick={handlePublish} disabled={publishing}>
                {publishing ? 'Publication...' : 'Publier'}
              </Button>
            </Stack>
          </Box>
        )}

        {/* Step 2b: Error */}
        {step === 'error' && (
          <Box sx={{ textAlign: 'center' }}>
            {preview && <img src={preview} style={{ maxWidth: '100%', maxHeight: 300,
              borderRadius: 8, marginBottom: 16 }} alt="preview" />}
            <Alert severity="error" sx={{ mb: 2 }}>
              Aucune canette de Monster détectée sur cette photo.
            </Alert>
            <Button variant="outlined" onClick={handleRetry}>Réessayer</Button>
          </Box>
        )}
      </Paper>
    </Box>
  )
}
