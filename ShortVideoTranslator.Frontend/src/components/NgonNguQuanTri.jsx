import { createContext, useContext } from 'react';
import { chuQuanTri } from "../data/tuDienQuanTri.js";
export const NgonNguQuanTri = createContext('vi');
export const useChuQuanTri = () => {
  const language = useContext(NgonNguQuanTri);
  return text => chuQuanTri(language, text);
};
