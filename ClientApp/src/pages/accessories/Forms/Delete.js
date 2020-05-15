import React, { useState } from "react";
import { Modal } from "react-bootstrap";


export function Delete(props) {
    const [show, setShow] = useState(false);
    return <>
        <button onClick={() => setShow(true)} className="btn btn-outline-danger">{props.delete}</button>
        <Modal id="delete_modal" show={show}>
            <Modal.Body>
                <div className="h5">{props.confirm}</div>
                <div className="float-right">
                    <button onClick={() => setShow(false)} className="btn btn-sm btn-secondary mr-2">{props.cancel}</button>
                    <button onClick={() => {setShow(false); props.onDelete();}} className="btn btn-sm btn-danger">{props.delete}</button>
                </div>
            </Modal.Body>
        </Modal>
    </>
}